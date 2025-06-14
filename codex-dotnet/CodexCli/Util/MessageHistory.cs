using System.Text.Json;
using CodexCli.Config;

namespace CodexCli.Util;

public static class MessageHistory
{
    private const string HistoryFile = "history.jsonl";

    private static string GetHistoryPath(AppConfig cfg)
    {
        var home = cfg.CodexHome ?? EnvUtils.FindCodexHome();
        return Path.Combine(home, HistoryFile);
    }

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
}
