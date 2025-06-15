using CodexCli.Models;

namespace CodexCli.Util;

public static class RolloutReplayer
{
    public static async IAsyncEnumerable<string> ReplayLinesAsync(string path)
    {
        if (!File.Exists(path)) yield break;
        await foreach (var line in File.ReadLinesAsync(path))
        {
            if (!string.IsNullOrWhiteSpace(line))
                yield return line;
        }
    }
}
