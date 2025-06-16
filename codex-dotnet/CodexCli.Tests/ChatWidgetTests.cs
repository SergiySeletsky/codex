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
}
