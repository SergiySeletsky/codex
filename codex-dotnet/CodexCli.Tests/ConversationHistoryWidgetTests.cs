using CodexCli.Interactive;
using CodexCli.Util;
using Xunit;
using System;
using System.IO;

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
        const string json = "{\"content\":[{\"type\":\"image\",\"data\":\"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAADUlEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg==\"}]}";
        hist.AddMcpToolCallImage(ToolResultUtils.FormatImageInfo(json));
        var line = Assert.Single(hist.GetVisibleLines(1));
        Assert.Equal("[magenta]tool[/] <image 1x1>", line);
    }

    [Fact]
    public void ToolImageEvent_Jpeg()
    {
        var hist = new ConversationHistoryWidget();
        const string jpeg = "/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDi6KKK+ZP3E//Z";
        hist.AddMcpToolCallImage(ToolResultUtils.FormatImageInfo($"{{\"content\":[{{\"type\":\"image\",\"data\":\"{jpeg}\"}}]}}"));
        var line = Assert.Single(hist.GetVisibleLines(1));
        Assert.Equal("[magenta]tool[/] <image 1x1>", line);
    }

    [Fact]
    public void UserImageIsStored()
    {
        var hist = new ConversationHistoryWidget();
        var path = Path.GetTempFileName() + ".png";
        File.WriteAllBytes(path, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAADUlEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="));
        hist.AddUserImage(path);
        var line = Assert.Single(hist.GetVisibleLines(1));
        Assert.Equal("[bold cyan]You:[/] <image 1x1>", line);
    }

    [Fact]
    public void UserImageIsStored_Jpeg()
    {
        var hist = new ConversationHistoryWidget();
        var path = Path.GetTempFileName() + ".jpg";
        File.WriteAllBytes(path, Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDi6KKK+ZP3E//Z"));
        hist.AddUserImage(path);
        var line = Assert.Single(hist.GetVisibleLines(1));
        Assert.Equal("[bold cyan]You:[/] <image 1x1>", line);
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
