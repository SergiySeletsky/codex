using System.Collections.Concurrent;

namespace CodexCli.Util;

public static class SessionManager
{
    private static readonly ConcurrentDictionary<string, List<string>> Sessions = new();

    public static string CreateSession()
    {
        var id = Guid.NewGuid().ToString("N");
        Sessions[id] = new List<string>();
        return id;
    }

    public static void AddEntry(string id, string line)
    {
        if (Sessions.TryGetValue(id, out var list))
            list.Add(line);
    }

    public static IReadOnlyList<string> GetHistory(string id) =>
        Sessions.TryGetValue(id, out var list) ? list : Array.Empty<string>();
}
