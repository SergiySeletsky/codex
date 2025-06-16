using CodexCli.Protocol;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class MockCodexAgentTests
{
    [Fact]
    public async Task EchoesPromptInMessage()
    {
        var list = new List<Event>();
        await foreach (var ev in MockCodexAgent.RunAsync("hi", new string[0]))
            list.Add(ev);
        Assert.Contains(list, e => e is AgentMessageEvent am && am.Message.Contains("hi"));
        Assert.Contains(list, e => e is TaskCompleteEvent);
    }
}
