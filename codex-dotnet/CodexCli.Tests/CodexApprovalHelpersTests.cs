using CodexCli.ApplyPatch;
using CodexCli.Util;
using CodexCli.Protocol;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class CodexApprovalHelpersTests
{
    [Fact]
    public async Task RequestCommandApproval_ReturnsEventAndCompletes()
    {
        var state = new CodexState();
        var (task, ev) = Codex.RequestCommandApproval(state, "1", new List<string>{"ls"}, "/tmp", null);
        Assert.True(state.PendingApprovals.ContainsKey("1"));
        Assert.Equal(new[]{"ls"}, ev.Command);
        Codex.NotifyApproval(state, "1", ReviewDecision.Approved);
        var decision = await task;
        Assert.Equal(ReviewDecision.Approved, decision);
    }

    [Fact]
    public async Task RequestPatchApproval_ReturnsEventAndCompletes()
    {
        var changes = new Dictionary<string, ApplyPatchFileChange>{
            ["a.txt"] = new ApplyPatchFileChange{ Kind = "add", Content = "" }
        };
        var action = new ApplyPatchAction(changes);
        var state = new CodexState();
        var (task, ev) = Codex.RequestPatchApproval(state, "2", action, null, null);
        Assert.True(ev.PatchSummary.Contains("a.txt"));
        Codex.NotifyApproval(state, "2", ReviewDecision.Denied);
        var decision = await task;
        Assert.Equal(ReviewDecision.Denied, decision);
    }

    [Fact]
    public void AddApprovedCommand_AddsToSet()
    {
        var state = new CodexState();
        Codex.AddApprovedCommand(state, new List<string>{"echo","hi"});
        Assert.Single(state.ApprovedCommands);
    }
}
