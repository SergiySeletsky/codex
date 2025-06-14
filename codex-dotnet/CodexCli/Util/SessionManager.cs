using System.Collections.Concurrent;
using System;
using System.IO;

namespace CodexCli.Util;

public static class SessionManager
{
    private static readonly ConcurrentDictionary<string, List<string>> Sessions = new();
    private static readonly ConcurrentDictionary<string, string> SessionFiles = new();

    public static string CreateSession()
    {
        var id = Guid.NewGuid().ToString("N");
        Sessions[id] = new List<string>();
        var dir = EnvUtils.GetHistoryDir();
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, id + ".txt");
        SessionFiles[id] = file;
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
}
