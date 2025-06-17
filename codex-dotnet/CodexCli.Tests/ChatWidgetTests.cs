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
}
