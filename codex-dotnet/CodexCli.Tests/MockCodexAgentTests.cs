using CodexCli.Protocol;
using System.Collections.Generic;
using System.Threading;
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

    [Fact]
    public async Task ApprovalResponderInvoked()
    {
        bool exec = false;
        bool patch = false;
        var list = new List<Event>();
        await foreach (var ev in MockCodexAgent.RunAsync("hi", new string[0], req =>
        {
            if (req is ExecApprovalRequestEvent) exec = true;
            if (req is PatchApplyApprovalRequestEvent) patch = true;
            return Task.FromResult(ReviewDecision.Approved);
        }))
            list.Add(ev);
        Assert.True(exec);
        Assert.True(patch);
        Assert.Contains(list, e => e is BackgroundEvent b && b.Message.Contains("exec_approval"));
        Assert.Contains(list, e => e is BackgroundEvent b && b.Message.Contains("patch_approval"));
    }

    [Fact]
    public async Task MockCodexAgentInterrupted()
    {
        using var cts = new CancellationTokenSource();
        var list = new List<Event>();
        await foreach (var ev in MockCodexAgent.RunAsync("hi", new string[0], null, cts.Token))
        {
            list.Add(ev);
            cts.Cancel();
        }
        Assert.DoesNotContain(list, e => e is TaskCompleteEvent);
        Assert.Contains(list, e => e is ErrorEvent err && err.Message.Contains("Interrupted"));
    }
}
