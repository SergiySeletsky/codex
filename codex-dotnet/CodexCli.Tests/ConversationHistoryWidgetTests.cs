using CodexCli.Interactive;
using Xunit;

public class ConversationHistoryWidgetTests
{
    [Fact]
    public void ScrollHistory()
    {
        var hist = new ConversationHistoryWidget();
        for (int i = 0; i < 5; i++)
            hist.AddSystemMessage($"line {i}");
        var lines = hist.GetVisibleLines(3);
        Assert.Equal(new[]{"[bold yellow]System:[/] line 2",
            "[bold yellow]System:[/] line 3",
            "[bold yellow]System:[/] line 4"}, lines);

        hist.ScrollUp(1);
        lines = hist.GetVisibleLines(3);
        Assert.Equal(new[]{"[bold yellow]System:[/] line 1",
            "[bold yellow]System:[/] line 2",
            "[bold yellow]System:[/] line 3"}, lines);

        hist.ScrollDown(1);
        lines = hist.GetVisibleLines(3);
        Assert.Equal(new[]{"[bold yellow]System:[/] line 2",
            "[bold yellow]System:[/] line 3",
            "[bold yellow]System:[/] line 4"}, lines);

        hist.ScrollPageUp(2);
        lines = hist.GetVisibleLines(3);
        Assert.Equal(new[]{"[bold yellow]System:[/] line 0",
            "[bold yellow]System:[/] line 1",
            "[bold yellow]System:[/] line 2"}, lines);

        hist.ScrollPageDown(2);
        lines = hist.GetVisibleLines(3);
        Assert.Equal(new[]{"[bold yellow]System:[/] line 2",
            "[bold yellow]System:[/] line 3",
            "[bold yellow]System:[/] line 4"}, lines);

        hist.ScrollToBottom();
        lines = hist.GetVisibleLines(3);
        Assert.Equal(new[]{"[bold yellow]System:[/] line 2",
            "[bold yellow]System:[/] line 3",
            "[bold yellow]System:[/] line 4"}, lines);
    }

    [Fact]
    public void CommandAndPatchEvents()
    {
        var hist = new ConversationHistoryWidget();
        hist.AddExecCommand("mkdir out");
        hist.AddExecResult(1);
        hist.AddPatchApplyBegin(false);
        hist.AddPatchApplyEnd(false);
        var lines = hist.GetVisibleLines(4);
        Assert.Contains("[magenta]exec[/] mkdir out", lines);
        Assert.Contains("[magenta]exec[/] exited 1", lines);
        Assert.Contains("[magenta]apply_patch[/] auto_approved=False", lines);
        Assert.Contains("[magenta]apply_patch[/] failed", lines);
    }

    [Fact]
    public void ToolCallEvents()
    {
        var hist = new ConversationHistoryWidget();
        hist.AddMcpToolCallBegin("srv", "tool", null);
        hist.AddMcpToolCallEnd(false, "{\"err\":1}");
        var lines = hist.GetVisibleLines(3);
        Assert.Contains("[magenta]tool[/] [bold]srv.tool()[/]", lines);
        Assert.Contains("[magenta]tool[/] failed:", lines);
        Assert.Contains("[dim]{\"err\": 1}[/]", lines);
    }

    [Fact]
    public void ToolImageEvent()
    {
        var hist = new ConversationHistoryWidget();
        hist.AddMcpToolCallImage();
        var line = Assert.Single(hist.GetVisibleLines(1));
        Assert.Equal("[magenta]tool[/] <image output>", line);
    }

    [Fact]
    public void ClearRemovesLines()
    {
        var hist = new ConversationHistoryWidget();
        hist.AddSystemMessage("line");
        Assert.Single(hist.GetVisibleLines(1));
        hist.Clear();
        Assert.Empty(hist.GetVisibleLines(1));
    }
}
