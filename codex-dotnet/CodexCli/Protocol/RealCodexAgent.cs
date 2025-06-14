using CodexCli.Util;

namespace CodexCli.Protocol;

public static class RealCodexAgent
{
    public static async IAsyncEnumerable<Event> RunAsync(string prompt, OpenAIClient client, string model)
    {
        yield return new SessionConfiguredEvent(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), model);
        var response = await client.ChatAsync(prompt);
        var msgId = Guid.NewGuid().ToString();
        yield return new AgentMessageEvent(msgId, response);
        yield return new TaskCompleteEvent(Guid.NewGuid().ToString(), response);
    }
}
