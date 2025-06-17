using CodexCli.Interactive;
using CodexCli.Protocol;
using Xunit;

public class ApprovalModalViewTests
{
    [Fact]
    public void ReturnsDecisionImmediately()
    {
        var modal = new ApprovalModalView(
            new ExecApprovalRequestEvent("1", new[]{"ls"}),
            () => "y");
        Assert.True(modal.IsComplete);
        Assert.Equal(ReviewDecision.Approved, modal.Decision);
    }

}
