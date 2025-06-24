// Ported from codex-rs/core/src/message_history.rs (done)
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using Mono.Unix;
using CodexCli.Config;

namespace CodexCli.Util;

/// <summary>
/// Persistence layer for the global append-only message history file.
/// Mirrors codex-rs/core/src/message_history.rs (watch-events parity tested).
/// </summary>
public static class MessageHistory
{
    private const string HistoryFile = "history.jsonl";

    private static string GetHistoryPath(AppConfig cfg)
    {
        var home = cfg.CodexHome ?? EnvUtils.FindCodexHome();
        return Path.Combine(home, HistoryFile);
    }

    public static string GetHistoryFile(AppConfig cfg) => GetHistoryPath(cfg);

    public class HistoryEntry
    {
        public string SessionId { get; set; } = string.Empty;
        public ulong Ts { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    private const int MaxRetries = 10;
    private static readonly TimeSpan RetrySleep = TimeSpan.FromMilliseconds(100);

    public static async Task AppendEntryAsync(string text, string sessionId, AppConfig cfg)
    {
        if (cfg.History.Persistence == HistoryPersistence.None)
            return;

        var path = GetHistoryPath(cfg);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var entry = new HistoryEntry
        {
            SessionId = sessionId,
            Ts = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Text = text
        };
        var lineBytes = System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry) + "\n");

        using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

        // Ensure permissions before locking/writing
        if (!OperatingSystem.IsWindows())
        {
            try { File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); } catch {}
        }

        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                fs.Lock(0, 0);
                break;
            }
            catch (IOException)
            {
                if (i == MaxRetries - 1)
                    throw;
                await Task.Delay(RetrySleep);
            }
        }

        fs.Seek(0, SeekOrigin.End);
        await fs.WriteAsync(lineBytes, 0, lineBytes.Length);
        await fs.FlushAsync();
        try { fs.Unlock(0, 0); } catch { }
    }

    private static ulong GetFileId(string path)
    {
        if (OperatingSystem.IsWindows())
            return 0;
        try
        {
            var info = new Mono.Unix.UnixFileInfo(path);
            return (ulong)info.Inode;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Ported from codex-rs/core/src/message_history.rs `history_metadata` (done).
    /// Returns the file identifier and line count.
    /// </summary>
    public static async Task<(ulong LogId, int Count)> HistoryMetadataAsync(AppConfig cfg)
    {
        var path = GetHistoryPath(cfg);
        if (!File.Exists(path)) return (0, 0);
        ulong logId = GetFileId(path);
        int count = 0;
        var buffer = new byte[8192];
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        int read;
        while ((read = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
                if (buffer[i] == (byte)'\n') count++;
        }
        return (logId, count);
    }

    public static async Task<int> CountEntriesAsync(AppConfig cfg)
    {
        var meta = await HistoryMetadataAsync(cfg);
        return meta.Count;
    }

    public static async Task<IEnumerable<string>> LastEntriesAsync(int count, AppConfig cfg)
    {
        var path = GetHistoryPath(cfg);
        if (!File.Exists(path)) return Enumerable.Empty<string>();
        var lines = await File.ReadAllLinesAsync(path);
        return lines.Reverse().Take(count).Select(line =>
        {
            try
            {
                var entry = JsonSerializer.Deserialize<HistoryEntry>(line);
                return entry?.Text ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }).Reverse();
    }

    public static async Task<IEnumerable<string>> SearchEntriesAsync(string term, AppConfig cfg)
    {
        var path = GetHistoryPath(cfg);
        if (!File.Exists(path)) return Enumerable.Empty<string>();
        var results = new List<string>();
        await foreach (var line in File.ReadLinesAsync(path))
        {
            if (line.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var entry = JsonSerializer.Deserialize<HistoryEntry>(line);
                    if (entry != null)
                        results.Add(entry.Text);
                }
                catch { }
            }
        }
        return results;
    }

    public static void ClearHistory(AppConfig cfg)
    {
        var path = GetHistoryPath(cfg);
        if (File.Exists(path)) File.Delete(path);
    }

    public static async Task<Dictionary<string,int>> SessionStatsAsync(AppConfig cfg)
    {
        var path = GetHistoryPath(cfg);
        var dict = new Dictionary<string,int>();
        if (!File.Exists(path)) return dict;
        await foreach (var line in File.ReadLinesAsync(path))
        {
            try
            {
                var entry = JsonSerializer.Deserialize<HistoryEntry>(line);
                if (entry != null)
                {
                    dict.TryGetValue(entry.SessionId, out var c);
                    dict[entry.SessionId] = c + 1;
                }
            }
            catch { }
        }
        return dict;
    }

    /// <summary>
    /// Ported from codex-rs/core/src/message_history.rs `lookup` (done).
    /// Returns the entry text for the given offset if the log id matches.
    /// </summary>
    public static string? LookupEntry(ulong logId, int offset, AppConfig cfg)
    {
        var path = GetHistoryPath(cfg);
        if (!File.Exists(path)) return null;
        if (GetFileId(path) != logId) return null;
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);
        for (int i = 0; ; i++)
        {
            var line = sr.ReadLine();
            if (line == null) return null;
            if (i == offset)
            {
                try
                {
                    var entry = JsonSerializer.Deserialize<HistoryEntry>(line);
                    return entry?.Text;
                }
                catch
                {
                    return null;
                }
            }
        }
    }

    public static async IAsyncEnumerable<string> WatchEntriesAsync(AppConfig cfg, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token = default)
    {
        var path = GetHistoryPath(cfg);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);
        while (!token.IsCancellationRequested)
        {
            var line = await sr.ReadLineAsync();
            if (line != null)
            {
                HistoryEntry? entry = null;
                try { entry = JsonSerializer.Deserialize<HistoryEntry>(line); } catch { }
                if (entry != null) yield return entry.Text;
            }
            else
            {
                await Task.Delay(500, token);
            }
        }
    }
}
