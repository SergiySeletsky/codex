using CodexCli.Interactive;
using CodexCli.Protocol;
using CodexCli.Config;
using CodexCli.Util;
using System.Collections.Generic;
using System;
using System.IO;
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
    public void AgentMessageMarkdownIsRewritten()
    {
        var widget = new ChatWidget(UriBasedFileOpener.VsCode, "/root");
        widget.AddAgentMessage("See 【F:a.rs†L1】");
        var line = Assert.Single(widget.GetVisibleLines(1));
        Assert.Equal("[bold green]Codex:[/] See [[a.rs:1]](vscode://file/root/a.rs:1) ", line);
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
    public void HandleScrollDeltaMagnifies()
    {
        var widget = new ChatWidget();
        for (int i = 0; i < 5; i++)
            widget.AddSystemMessage($"line {i}");

        widget.HandleScrollDelta(-1);
        var lines = widget.GetVisibleLines(3);
        Assert.Equal(new[]{"[bold yellow]System:[/] line 0",
            "[bold yellow]System:[/] line 1",
            "[bold yellow]System:[/] line 2"}, lines);

        widget.HandleScrollDelta(1);
        lines = widget.GetVisibleLines(3);
        Assert.Equal(new[]{"[bold yellow]System:[/] line 1",
            "[bold yellow]System:[/] line 2",
            "[bold yellow]System:[/] line 3"}, lines);
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

    [Fact]
    public void ToolCallEventsAreStored()
    {
        var widget = new ChatWidget();
        widget.AddMcpToolCallBegin("srv", "tool", "{\"a\":1}");
        widget.AddMcpToolCallEnd(true, "{\"ok\":true}");
        var lines = widget.GetVisibleLines(3);
        Assert.Contains("[magenta]tool[/] [bold]srv.tool({\"a\":1})[/]", lines);
        Assert.Contains("[magenta]tool[/] success:", lines);
        Assert.Contains("[dim]{\"ok\": true}[/]", lines);
    }

    [Fact]
    public void ToolImageEventIsStored()
    {
        var widget = new ChatWidget();
        const string json = "{\"content\":[{\"type\":\"image\",\"data\":\"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAADUlEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg==\"}]}";
        widget.AddMcpToolCallImage(json);
        var line = Assert.Single(widget.GetVisibleLines(1));
        Assert.Equal("[magenta]tool[/] <image 1x1>", line);
    }

    [Fact]
    public void ToolImageEvent_Jpeg()
    {
        var widget = new ChatWidget();
        const string jpeg = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDi6KKK+ZP3E//Z";
        widget.AddMcpToolCallImage($"{{\"content\":[{{\"type\":\"image\",\"data\":\"{jpeg}\"}}]}}");
        var line = Assert.Single(widget.GetVisibleLines(1));
        Assert.Equal("[magenta]tool[/] <image 1x1>", line);
    }

    [Fact]
    public void UserImageIsStored()
    {
        var widget = new ChatWidget();
        var path = Path.GetTempFileName() + ".png";
        File.WriteAllBytes(path, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAADUlEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="));
        widget.AddUserImage(path);
        var line = Assert.Single(widget.GetVisibleLines(1));
        Assert.Equal("[bold cyan]You:[/] <image 1x1>", line);
    }

    [Fact]
    public void UserImageIsStored_Jpeg()
    {
        var widget = new ChatWidget();
        var path = Path.GetTempFileName() + ".jpg";
        File.WriteAllBytes(path, Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDi6KKK+ZP3E//Z"));
        widget.AddUserImage(path);
        var line = Assert.Single(widget.GetVisibleLines(1));
        Assert.Equal("[bold cyan]You:[/] <image 1x1>", line);
    }

    [Fact]
    public void ClearConversationRemovesLines()
    {
        var widget = new ChatWidget();
        widget.AddAgentMessage("hello");
        Assert.Single(widget.GetVisibleLines(1));
        widget.ClearConversation();
        Assert.Empty(widget.GetVisibleLines(1));
    }

    [Fact]
    public void LayoutHeightsReserveSpacing()
    {
        var widget = new ChatWidget();
        var (chat, bottom) = widget.GetLayoutHeights(10);
        Assert.Equal(9 - bottom, chat); // one line spacing
    }

    [Fact]
    public void LayoutClampsBottomHeight()
    {
        var widget = new ChatWidget();
        var (chat, bottom) = widget.GetLayoutHeights(4);
        Assert.Equal(2, chat);
        Assert.Equal(1, bottom);
    }
}
