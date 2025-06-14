using CodexCli.Protocol;

namespace CodexCli.Protocol;

public static class MockCodexAgent
{
    public static async IAsyncEnumerable<Event> RunAsync(string prompt)
    {
        var id = Guid.NewGuid().ToString();
        await Task.Delay(100);
        yield return new AgentMessageEvent(id, $"Echoing: {prompt.Trim()}");
        await Task.Delay(100);
        yield return new TaskCompleteEvent(Guid.NewGuid().ToString(), $"{prompt.Trim()} done");
    }
}
