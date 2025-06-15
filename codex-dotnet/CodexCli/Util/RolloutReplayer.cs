using CodexCli.Models;
using System.Collections.Generic;
using System.IO;

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

    public static async IAsyncEnumerable<ResponseItem> ReplayAsync(string path)
    {
        await foreach (var line in ReplayLinesAsync(path))
        {
            ResponseItem? item = null;
            try { item = System.Text.Json.JsonSerializer.Deserialize<ResponseItem>(line); }
            catch { }
            if (item != null) yield return item;
        }
    }
}
