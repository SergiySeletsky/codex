using CodexCli.Util;
using System;
using System.Threading;
using System.Threading.Tasks;

/// Ported from codex-rs/core/src/client.rs (streaming model client with Ctrl+C parity)

namespace CodexCli.Protocol;

public static class RealCodexAgent
{
    public static async IAsyncEnumerable<Event> RunAsync(string prompt, OpenAIClient client, string model,
        Func<Event, Task<ReviewDecision>>? approvalResponder = null, IReadOnlyList<string>? images = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancel = default)
    {
        yield return new SessionConfiguredEvent(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), model);
        var msgId = Guid.NewGuid().ToString();
        var full = new System.Text.StringBuilder();
        bool interrupted = false;
        await foreach (var chunk in client.ChatStreamAsync(prompt, cancel).WithCancellation(cancel))
        {
            if (cancel.IsCancellationRequested)
            {
                interrupted = true;
                break;
            }
            full.Append(chunk);
            yield return new AgentMessageEvent(msgId, chunk);
        }
        if (interrupted || cancel.IsCancellationRequested)
        {
            yield return new ErrorEvent(Guid.NewGuid().ToString(), "Interrupted");
        }
        else
        {
            yield return new TaskCompleteEvent(Guid.NewGuid().ToString(), full.ToString());
        }
    }
}
