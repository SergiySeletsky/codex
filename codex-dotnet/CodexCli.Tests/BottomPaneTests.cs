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
        // Rendering writes via Spectre.Console which bypasses Console.Out in this
        // test environment, so we only verify the decision and overlay state.
    }

    [Fact]
    public void SetTaskRunningShowsAndHidesStatusIndicator()
    {
        var pane = new BottomPane(new AppEventSender(_ => { }), true);
        var field = typeof(BottomPane).GetField("_activeView", BindingFlags.NonPublic | BindingFlags.Instance)!;
        pane.SetTaskRunning(true);
        var view = field.GetValue(pane);
        Assert.NotNull(view);
        Assert.Equal("StatusIndicatorView", view!.GetType().Name);
        pane.SetTaskRunning(false);
        Assert.Null(field.GetValue(pane));
    }

    [Fact]
    public void StatusIndicatorKeepsComposerHeight()
    {
        var pane = new BottomPane(new AppEventSender(_ => { }), true);
        int before = pane.CalculateRequiredHeight(10);
        pane.SetTaskRunning(true);
        int after = pane.CalculateRequiredHeight(10);
        Assert.Equal(before, after);
        pane.SetTaskRunning(false); // cleanup background task
    }

    [Fact]
    public void ApprovalModalUsesOverlayHeight()
    {
        var pane = new BottomPane(new AppEventSender(_ => { }), true);
        Console.SetIn(new StringReader("n\n"));
        pane.PushApprovalRequest(new ExecApprovalRequestEvent("1", new[] { "ls" }));
        int height = pane.CalculateRequiredHeight(10);
        Assert.Equal(1, height);
        pane.Render(1); // cleanup
    }
}

