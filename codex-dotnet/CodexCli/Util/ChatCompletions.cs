using CodexCli.Models;
using System.Collections.Generic;
using System.Threading;

namespace CodexCli.Util;

// Ported from codex-rs/core/src/chat_completions.rs aggregator (done)
public static class ChatCompletions
{
    public static async IAsyncEnumerable<ResponseEvent> Aggregate(
        this IAsyncEnumerable<ResponseEvent> source,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancel = default)
    {
        string cumulative = string.Empty;
        Completed? pending = null;
        await foreach (var ev in source.WithCancellation(cancel))
        {
            if (pending != null)
            {
                yield return pending;
                pending = null;
            }
            switch (ev)
            {
                case OutputItemDone { Item: MessageItem msg } when msg.Role == "assistant":
                    var text = msg.Content.Count > 0 ? msg.Content[0].Text : string.Empty;
                    cumulative += text;
                    continue;
                case Completed c:
                    if (!string.IsNullOrEmpty(cumulative))
                    {
                        var item = new MessageItem("assistant",
                            new List<ContentItem>{ new("output_text", cumulative) });
                        cumulative = string.Empty;
                        yield return new OutputItemDone(item);
                        pending = c;
                        continue;
                    }
                    break;
            }
            yield return ev;
        }
        if (pending != null)
            yield return pending;
    }

    public static ResponseStream Aggregate(this ResponseStream stream)
    {
        var aggregated = new ResponseStream();
        _ = Task.Run(async () =>
        {
            await foreach (var ev in stream.Aggregate())
                aggregated.Writer.TryWrite(ev);
            aggregated.Writer.Complete();
        });
        return aggregated;
    }
}

