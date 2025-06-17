using System;
using System.IO;
using System.Reflection;
using CodexCli.Interactive;
using CodexCli.Protocol;
using Xunit;

public class BottomPaneTests
{
    [Fact]
    public void ApprovalRequestRendersDecisionAndClearsOverlay()
    {
        var sender = new AppEventSender(_ => { });
        var pane = new BottomPane(sender, hasInputFocus: true);
        Console.SetIn(new StringReader("y\n"));
        var sw = new StringWriter();
        Console.SetOut(sw);

        var decision = pane.PushApprovalRequest(new ExecApprovalRequestEvent("1", new[] { "ls" }));

        var field = typeof(BottomPane).GetField("_activeView", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field!.GetValue(pane));
        pane.Render(1);
        Assert.Null(field.GetValue(pane));
        Assert.Equal(ReviewDecision.Approved, decision);
        Assert.Contains("Approved", sw.ToString());
    }
}

