// Ported from codex-rs/core/src/message_history.rs (done)
using System.Text.Json;
using System.Collections.Generic;
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
        var line = JsonSerializer.Serialize(entry) + "\n";
        await File.AppendAllTextAsync(path, line);
        if (!OperatingSystem.IsWindows())
        {
            try { File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); } catch {}
        }
    }

    public static async Task<(ulong LogId, int Count)> HistoryMetadataAsync(AppConfig cfg)
    {
        var path = GetHistoryPath(cfg);
        if (!File.Exists(path)) return (0, 0);
        int count = 0;
        await foreach (var _ in File.ReadLinesAsync(path))
            count++;
        return (0UL, count);
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

    public static string? LookupEntry(ulong logId, int offset, AppConfig cfg)
    {
        var path = GetHistoryPath(cfg);
        if (!File.Exists(path)) return null;
        using var sr = new StreamReader(path);
        for (int i = 0; i <= offset; i++)
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
        return null;
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
