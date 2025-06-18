using CodexCli.Interactive;
using CodexCli.Protocol;
using System.Collections.Generic;
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
    public void AgentReasoningIsStored()
    {
        var widget = new ChatWidget();
        widget.AddAgentReasoning("thinking...");
        var lines = widget.GetVisibleLines(1);
        Assert.Contains("[italic]thinking...[/]", lines);
    }

    [Fact]
    public void BackgroundAndErrorMessagesAreStored()
    {
        var widget = new ChatWidget();
        widget.AddBackgroundEvent("upload finished");
        widget.AddError("oops");
        var lines = widget.GetVisibleLines(2);
        Assert.Contains("[gray]upload finished[/]", lines);
        Assert.Contains("[red]ERROR: oops[/]", lines);
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

    [Fact]
    public void HistoryEntriesAreStored()
    {
        var widget = new ChatWidget();
        widget.AddHistoryEntry(2, "old");
        var lines = widget.GetVisibleLines(1);
        Assert.Contains("[dim]history 2: old[/]", lines);
    }

    [Fact]
    public void CommandAndPatchEventsAreStored()
    {
        var widget = new ChatWidget();
        widget.AddExecCommand("touch file.txt");
        widget.AddExecResult(0);
        widget.AddPatchApplyBegin(true);
        widget.AddPatchApplyEnd(true);
        var lines = widget.GetVisibleLines(4);
        Assert.Contains("[magenta]exec[/] touch file.txt", lines);
        Assert.Contains("[magenta]exec[/] succeeded", lines);
        Assert.Contains("[magenta]apply_patch[/] auto_approved=True", lines);
        Assert.Contains("[magenta]apply_patch[/] succeeded", lines);
    }

    [Fact]
    public void PatchSummaryLinesAreRendered()
    {
        var widget = new ChatWidget();
        var changes = new Dictionary<string,FileChange>
        {
            {"a.txt", new AddFileChange("hello\nworld\n")},
            {"b.txt", new DeleteFileChange()},
            {"c.txt", new UpdateFileChange("+hi\n-context\n", null)}
        };
        widget.AddPatchApplyBegin(true, changes);
        var lines = widget.GetVisibleLines(8);
        Assert.Contains("[magenta]applying patch[/]", lines);
        Assert.Contains("[green bold]A[/] a.txt (+2)", lines);
        Assert.Contains("[red bold]D[/] b.txt", lines);
        Assert.Contains("[yellow bold]M[/] c.txt", lines);
        Assert.Contains("[green]+hi[/]", lines);
    }
}
