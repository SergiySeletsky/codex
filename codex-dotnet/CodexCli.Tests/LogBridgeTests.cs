using CodexCli.Interactive;
using System;
using System.Reflection;
using Xunit;

public class LogBridgeTests
{
    [Fact]
    public void LatestLogUpdatesStatusIndicator()
    {
        var widget = new ChatWidget();
        widget.SetTaskRunning(true);
        LogBridge.LatestLog += widget.UpdateLatestLog;
        try
        {
            LogBridge.Emit("working");
            var pane = typeof(ChatWidget).GetField("_bottomPane", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(widget);
            var view = pane!.GetType().GetField("_activeView", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(pane);
            var w = view!.GetType().GetField("_widget", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(view);
            var text = (string)w!.GetType().GetField("_text", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(w)!;
            Assert.Equal("working", text);
        }
        finally
        {
            LogBridge.LatestLog -= widget.UpdateLatestLog;
        }
    }
}
