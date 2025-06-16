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
        Assert.Contains(list, e => e is ExecCommandBeginEvent);
        Assert.Contains(list, e => e is ExecCommandEndEvent);
        Assert.Contains(list, e => e is PatchApplyBeginEvent);
        Assert.Contains(list, e => e is PatchApplyEndEvent);
        Assert.Contains(list, e => e is McpToolCallBeginEvent);
        Assert.Contains(list, e => e is McpToolCallEndEvent);
        Assert.Contains(list, e => e is ExecApprovalRequestEvent);
        Assert.Contains(list, e => e is PatchApplyApprovalRequestEvent);
    }
}
