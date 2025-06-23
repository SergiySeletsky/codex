using CodexCli.Util;
using CodexCli.Protocol;
using CodexCli.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class CodexAbortTests
{
    [Fact]
    public void AbortClearsStateAndCancelsTask()
    {
        var state = new CodexState();
        bool aborted = false;
        var task = new AgentTask("sub", () => aborted = true);
        Codex.SetTask(state, task);
        state.PendingInput.Add(new MessageInputItem("assistant", new List<ContentItem>()));
        state.PendingApprovals["x"] = new TaskCompletionSource<ReviewDecision>();

        Codex.Abort(state);

        Assert.Empty(state.PendingInput);
        Assert.Empty(state.PendingApprovals);
        Assert.False(state.HasCurrentTask);
        Assert.Null(state.CurrentTask);
        Assert.True(aborted);
    }
}
