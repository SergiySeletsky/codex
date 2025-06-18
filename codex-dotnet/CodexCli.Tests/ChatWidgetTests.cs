using CodexCli.Interactive;
using Xunit;

public class ChatWidgetTests
{
    [Fact]
    public void SystemMessagesAreStored()
    {
        var widget = new ChatWidget();
        widget.AddUserMessage("hi\u001b[31m");
        widget.AddAgentMessage("hello");
        widget.AddSystemMessage("test");

        var lines = widget.GetVisibleLines(3);
        Assert.Contains("[bold cyan]You:[/] hi", lines);
        Assert.Contains("[bold green]Codex:[/] hello", lines);
        Assert.Contains("[bold yellow]System:[/] test", lines);
    }

    [Fact]
    public void PagingScrollsHistory()
    {
        var widget = new ChatWidget();
        for (int i = 0; i < 5; i++)
            widget.AddSystemMessage($"line {i}");

        var lines = widget.GetVisibleLines(3);
        Assert.Equal(new[]{"[bold yellow]System:[/] line 2",
            "[bold yellow]System:[/] line 3",
            "[bold yellow]System:[/] line 4"}, lines);

        widget.ScrollPageUp(2);
        lines = widget.GetVisibleLines(3);
        Assert.Equal(new[]{"[bold yellow]System:[/] line 0",
            "[bold yellow]System:[/] line 1",
            "[bold yellow]System:[/] line 2"}, lines);

        widget.ScrollPageDown(2);
        lines = widget.GetVisibleLines(3);
        Assert.Equal(new[]{"[bold yellow]System:[/] line 2",
            "[bold yellow]System:[/] line 3",
            "[bold yellow]System:[/] line 4"}, lines);

        widget.ScrollUp(1);
        widget.ScrollToBottom();
        lines = widget.GetVisibleLines(3);
        Assert.Equal(new[]{"[bold yellow]System:[/] line 2",
            "[bold yellow]System:[/] line 3",
            "[bold yellow]System:[/] line 4"}, lines);
    }

    [Fact]
    public void TabSwitchesFocus()
    {
        var widget = new ChatWidget();
        for (int i = 0; i < 3; i++)
            widget.AddSystemMessage($"line {i}");

        widget.HandleKeyEvent(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
        widget.HandleKeyEvent(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        var lines = widget.GetVisibleLines(2);
        Assert.Equal(new[]{"[bold yellow]System:[/] line 0",
            "[bold yellow]System:[/] line 1"}, lines);
    }

    [Fact]
    public void TaskRunningShowsStatusIndicator()
    {
        var widget = new ChatWidget();
        var field = typeof(ChatWidget)
            .GetField("_bottomPane", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var pane = field.GetValue(widget);
        var active = pane!.GetType().GetField("_activeView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        widget.SetTaskRunning(true);
        Assert.NotNull(active.GetValue(pane));
        widget.SetTaskRunning(false);
        Assert.Null(active.GetValue(pane));
    }
}
