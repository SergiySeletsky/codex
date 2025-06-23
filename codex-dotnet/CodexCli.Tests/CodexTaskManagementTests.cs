using CodexCli.Util;
using Xunit;

public class CodexTaskManagementTests
{
    [Fact]
    public void SetTask_ReplacesExistingTaskAndAbortsOld()
    {
        var state = new CodexState();
        bool aborted1 = false;
        var t1 = new AgentTask("1", () => aborted1 = true);
        Codex.SetTask(state, t1);
        Assert.True(state.HasCurrentTask);
        Assert.Equal(t1, state.CurrentTask);

        bool aborted2 = false;
        var t2 = new AgentTask("2", () => aborted2 = true);
        Codex.SetTask(state, t2);
        Assert.True(aborted1);
        Assert.False(aborted2);
        Assert.Equal(t2, state.CurrentTask);
    }

    [Fact]
    public void RemoveTask_MatchingSubIdClearsTask()
    {
        var state = new CodexState();
        var t1 = new AgentTask("abc", () => { });
        Codex.SetTask(state, t1);
        Codex.RemoveTask(state, "abc");
        Assert.False(state.HasCurrentTask);
        Assert.Null(state.CurrentTask);
    }

    [Fact]
    public void RemoveTask_IgnoresDifferentSubId()
    {
        var state = new CodexState();
        var t1 = new AgentTask("abc", () => { });
        Codex.SetTask(state, t1);
        Codex.RemoveTask(state, "xyz");
        Assert.True(state.HasCurrentTask);
        Assert.Equal(t1, state.CurrentTask);
    }
}
