using CodexCli.Util;
using System;
using System.Threading.Tasks;

namespace CodexCli.Protocol;

public static class RealCodexAgent
{
    public static async IAsyncEnumerable<Event> RunAsync(string prompt, OpenAIClient client, string model,
        Func<Event, Task<ReviewDecision>>? approvalResponder = null)
    {
        yield return new SessionConfiguredEvent(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), model);
        var msgId = Guid.NewGuid().ToString();
        var full = new System.Text.StringBuilder();
        await foreach (var chunk in client.ChatStreamAsync(prompt))
        {
            full.Append(chunk);
            yield return new AgentMessageEvent(msgId, chunk);
        }
        yield return new TaskCompleteEvent(Guid.NewGuid().ToString(), full.ToString());
    }
}
