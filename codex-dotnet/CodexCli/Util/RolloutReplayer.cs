using CodexCli.Models;
using System.Collections.Generic;
using System.IO;

namespace CodexCli.Util;

public static class RolloutReplayer
{
    public static async IAsyncEnumerable<string> ReplayLinesAsync(string path, bool follow = false)
    {
        if (!File.Exists(path)) yield break;
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line))
                yield return line;
        }
        while (follow)
        {
            await Task.Delay(500);
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    yield return line;
            }
        }
    }

    public static async IAsyncEnumerable<ResponseItem> ReplayAsync(string path, bool follow = false)
    {
        await foreach (var line in ReplayLinesAsync(path, follow))
        {
            var item = ResponseItemFactory.FromJson(line);
            if (item != null) yield return item;
        }
    }
}
