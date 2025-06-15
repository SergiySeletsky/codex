using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using CodexCli.Protocol;
using CodexCli.Models;

namespace CodexCli.Util;

public static class McpEventStream
{
    public static async IAsyncEnumerable<string> ReadLinesAsync(string baseUrl, [EnumeratorCancellation] CancellationToken token = default)
    {
        using var http = new HttpClient();
        using var resp = await http.GetAsync($"{baseUrl.TrimEnd('/')}/events", HttpCompletionOption.ResponseHeadersRead, token);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(token);
        using var reader = new StreamReader(stream);
        var buffer = new StringBuilder();
        while (!token.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;
            if (line.StartsWith(":"))
            {
                // comment line - ignore
                continue;
            }
            if (line.StartsWith("data:"))
            {
                buffer.AppendLine(line.Substring(5).Trim());
            }
            else if (line.StartsWith("id:"))
            {
                // we currently ignore the id value
            }
            else if (line.StartsWith("event:"))
            {
                // event type is ignored for now
            }
            else if (string.IsNullOrEmpty(line))
            {
                if (buffer.Length > 0)
                {
                    yield return buffer.ToString().TrimEnd();
                    buffer.Clear();
                }
            }
        }
        if (buffer.Length > 0)
            yield return buffer.ToString().TrimEnd();
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

