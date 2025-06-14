using System.Collections.Concurrent;
using System;
using System.IO;

namespace CodexCli.Util;

public static class SessionManager
{
    private static readonly ConcurrentDictionary<string, List<string>> Sessions = new();
    private static readonly ConcurrentDictionary<string, string> SessionFiles = new();
    private static readonly ConcurrentDictionary<string, DateTime> SessionStarts = new();

    public static string CreateSession()
    {
        var id = Guid.NewGuid().ToString("N");
        Sessions[id] = new List<string>();
        var dir = EnvUtils.GetHistoryDir();
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, id + ".txt");
        SessionFiles[id] = file;
        SessionStarts[id] = DateTime.UtcNow;
        File.WriteAllText(file, $"# started {SessionStarts[id]:o}\n");
        return id;
    }

    public static void AddEntry(string id, string line)
    {
        if (Sessions.TryGetValue(id, out var list))
        {
            list.Add(line);
            if (SessionFiles.TryGetValue(id, out var path))
                File.AppendAllText(path, line + Environment.NewLine);
        }
    }

    public static IReadOnlyList<string> GetHistory(string id)
    {
        if (Sessions.TryGetValue(id, out var list))
            return list;
        if (SessionFiles.TryGetValue(id, out var path) && File.Exists(path))
            return File.ReadAllLines(path);
        return Array.Empty<string>();
    }

    public static void ClearHistory(string id)
    {
        Sessions.TryRemove(id, out _);
        SessionFiles.TryRemove(id, out var path);
        if (path != null && File.Exists(path))
            File.Delete(path);
        Sessions[id] = new List<string>();
        var dir = EnvUtils.GetHistoryDir();
        Directory.CreateDirectory(dir);
        SessionFiles[id] = Path.Combine(dir, id + ".txt");
    }

    public static string? GetHistoryFile(string id) =>
        SessionFiles.TryGetValue(id, out var path) ? path : null;

    public static DateTime? GetStartTime(string id) =>
        SessionStarts.TryGetValue(id, out var dt) ? dt : null;

    public static IEnumerable<string> ListSessions()
    {
        var dir = EnvUtils.GetHistoryDir();
        if (!Directory.Exists(dir))
            yield break;
        foreach (var file in Directory.GetFiles(dir, "*.txt"))
            yield return Path.GetFileNameWithoutExtension(file);
    }

    public static IEnumerable<(string Id, DateTime Start)> ListSessionsWithInfo()
    {
        foreach (var id in ListSessions())
        {
            var start = GetStartTime(id) ?? File.GetCreationTimeUtc(GetHistoryFile(id)!);
            yield return (id, start);
        }
    }

    public static string? GetHistoryEntry(string id, int offset)
    {
        var path = GetHistoryFile(id);
        if (path == null || !File.Exists(path)) return null;
        return File.ReadLines(path).Skip(offset).FirstOrDefault();
    }

    public static bool DeleteSession(string id)
    {
        var removed = Sessions.TryRemove(id, out _);
        if (SessionFiles.TryRemove(id, out var path) || removed)
        {
            if (path == null)
                path = Path.Combine(EnvUtils.GetHistoryDir(), id + ".txt");
            if (File.Exists(path))
                File.Delete(path);
            SessionStarts.TryRemove(id, out _);
            return true;
        }
        return false;
    }

    public static void DeleteAllSessions()
    {
        foreach (var id in ListSessions().ToArray())
        {
            DeleteSession(id);
        }
    }
}
