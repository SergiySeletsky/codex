using CodexCli.Interactive;
using CodexCli.Protocol;
using Xunit;

public class UserApprovalWidgetTests
{
    [Fact]
    public void ApproveExecWhenYes()
    {
        var widget = new UserApprovalWidget(() => "y");
        var dec = widget.PromptExec(new[]{"ls","-l"}, "/tmp", null);
        Assert.Equal(ReviewDecision.Approved, dec);
    }

    [Fact]
    public void AbortPatchWhenQ()
    {
        var widget = new UserApprovalWidget(() => "q");
        var dec = widget.PromptPatch("patch summary");
        Assert.Equal(ReviewDecision.Abort, dec);
    }
}
