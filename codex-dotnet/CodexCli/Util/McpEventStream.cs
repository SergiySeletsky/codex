using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CodexCli.Protocol;
using CodexCli.Models;

namespace CodexCli.Util;

public static class McpEventStream
{
    public static async IAsyncEnumerable<string> ReadLinesAsync(string baseUrl, [EnumeratorCancellation] CancellationToken token = default)
    {
        using var http = new HttpClient();
        using var stream = await http.GetStreamAsync($"{baseUrl.TrimEnd('/')}/events", token);
        using var reader = new StreamReader(stream);
        while (!token.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;
            if (line.StartsWith("data:"))
                yield return line.Substring(5).Trim();
        }
    }

public static async IAsyncEnumerable<Event> ReadEventsAsync(string baseUrl, [EnumeratorCancellation] CancellationToken token = default)
    {
        await foreach (var json in ReadLinesAsync(baseUrl, token))
        {
            Event? ev = null;
            try { ev = JsonSerializer.Deserialize<Event>(json); }
            catch { }
            if (ev != null) yield return ev;
        }
    }

    public static async IAsyncEnumerable<ResponseItem> ReadItemsAsync(string baseUrl, [EnumeratorCancellation] CancellationToken token = default)
    {
        await foreach (var ev in ReadEventsAsync(baseUrl, token))
        {
            var item = ResponseItemFactory.FromEvent(ev);
            if (item != null) yield return item;
        }
    }
}

