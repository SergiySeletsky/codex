using CodexCli.Interactive;
using CodexCli.Protocol;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class InteractiveAppApprovalTests
{
    [Fact]
    public async Task ApprovalHandlerPropertyInvoked()
    {
        List<Event> list = new();
        InteractiveApp.ApprovalHandler = _ => Task.FromResult(ReviewDecision.Approved);
        await foreach (var ev in MockCodexAgent.RunAsync("hi", new string[0], InteractiveApp.ApprovalHandler))
            list.Add(ev);
        InteractiveApp.ApprovalHandler = null;
        Assert.Contains(list, e => e is BackgroundEvent b && b.Message.Contains("exec_approval:Approved"));
        Assert.Contains(list, e => e is BackgroundEvent b && b.Message.Contains("patch_approval:Approved"));
    }
}
