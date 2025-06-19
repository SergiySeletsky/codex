using System;
using Spectre.Console;
using CodexCli.Util;
using CodexCli.Protocol;
using CodexCli.Config;

namespace CodexCli.Interactive;

/// <summary>
/// Chat widget managing conversation history and bottom pane input.
/// Mirrors codex-rs/tui/src/chatwidget.rs
/// (status indicator, log bridge, agent reasoning, background/error and history entry updates done.
/// exec command, patch diff summary, mcp tool call events with image detection
/// and PNG/JPEG dimension rendering, initial and interactive image prompts
/// handled, markdown history rendering, /new command clearing history,
/// layout spacing between history and composer and bottom pane height
/// clamping done).
/// </summary>
public class ChatWidget
{
    private readonly ConversationHistoryWidget _history;
    private readonly BottomPane _bottomPane;
    private InputFocus _focus = InputFocus.BottomPane;
    private const int LayoutSpacing = 1;

    private enum InputFocus { HistoryPane, BottomPane }

    public ChatWidget(UriBasedFileOpener opener = UriBasedFileOpener.None, string? cwd = null)
        : this(new AppEventSender(_ => { }), opener, cwd) {}

    public ChatWidget(AppEventSender sender, UriBasedFileOpener opener = UriBasedFileOpener.None, string? cwd = null)
    {
        _history = new ConversationHistoryWidget(opener, cwd);
        _bottomPane = new BottomPane(sender, hasInputFocus: true, _history);
    }

    public void AddUserMessage(string text)
    {
        _history.AddUserMessage(text);
        _history.ScrollToBottom();
        var clean = AnsiEscape.StripAnsi(text);
        AnsiConsole.MarkupLine($"[bold cyan]You:[/] {clean}");
    }

    public void AddUserImage(string path)
    {
        _history.AddUserImage(path);
        _history.ScrollToBottom();
        var desc = ToolResultUtils.FormatImageInfoFromFile(path);
        AnsiConsole.MarkupLine($"[bold cyan]You:[/] {desc}");
    }

    public void AddAgentMessage(string text)
    {
        _history.AddAgentMessage(text);
        _history.ScrollToBottom();
        var clean = AnsiEscape.StripAnsi(text);
        AnsiConsole.MarkupLine($"[bold green]Codex:[/] {clean}");
    }

    public void AddSystemMessage(string text)
    {
        _history.AddSystemMessage(text);
        _history.ScrollToBottom();
        var clean = AnsiEscape.StripAnsi(text);
        AnsiConsole.MarkupLine($"[bold yellow]System:[/] {clean}");
    }

    public void AddBackgroundEvent(string text)
    {
        _history.AddBackgroundEvent(text);
        _history.ScrollToBottom();
        var clean = AnsiEscape.StripAnsi(text);
        AnsiConsole.MarkupLine($"[gray]{Markup.Escape(clean)}[/]");
    }

    public void AddError(string text)
    {
        _history.AddError(text);
        _history.ScrollToBottom();
        var clean = AnsiEscape.StripAnsi(text);
        AnsiConsole.MarkupLine($"[red]ERROR: {Markup.Escape(clean)}[/]");
    }

    public void AddAgentReasoning(string text)
    {
        _history.AddAgentReasoning(text);
        _history.ScrollToBottom();
        var clean = AnsiEscape.StripAnsi(text);
        AnsiConsole.MarkupLine($"[italic]{Markup.Escape(clean)}[/]");
    }

    public void AddHistoryEntry(int offset, string text)
    {
        _history.AddHistoryEntry(offset, text);
        _history.ScrollToBottom();
        var clean = AnsiEscape.StripAnsi(text);
        AnsiConsole.MarkupLine($"[dim]history {offset}: {Markup.Escape(clean)}[/]");
    }

    public void ClearConversation()
    {
        _history.Clear();
    }

    public void AddExecCommand(string command)
    {
        _history.AddExecCommand(command);
        _history.ScrollToBottom();
        AnsiConsole.MarkupLine($"[magenta]exec[/] {Markup.Escape(command)}");
    }

    public void AddExecResult(int exitCode)
    {
        _history.AddExecResult(exitCode);
        _history.ScrollToBottom();
        var status = exitCode == 0 ? "succeeded" : $"exited {exitCode}";
        AnsiConsole.MarkupLine($"[magenta]exec[/] {status}");
    }

    public void AddPatchApplyBegin(bool autoApproved, IReadOnlyDictionary<string,FileChange>? changes = null)
    {
        _history.AddPatchApplyBegin(autoApproved, changes);
        _history.ScrollToBottom();

        if (changes == null)
        {
            AnsiConsole.MarkupLine($"[magenta]apply_patch[/] auto_approved={autoApproved}");
            return;
        }

        AnsiConsole.MarkupLine("[magenta]applying patch[/]");
        foreach (var line in ConversationHistoryWidget.FormatPatchLines(changes))
            AnsiConsole.MarkupLine(line);
    }

    public void AddPatchApplyEnd(bool success)
    {
        _history.AddPatchApplyEnd(success);
        _history.ScrollToBottom();
        var status = success ? "succeeded" : "failed";
        AnsiConsole.MarkupLine($"[magenta]apply_patch[/] {status}");
    }

    public void AddMcpToolCallBegin(string server, string tool, string? args)
    {
        _history.AddMcpToolCallBegin(server, tool, args);
        _history.ScrollToBottom();
        string invocation = $"{server}.{tool}" + (string.IsNullOrEmpty(args) ? "()" : $"({Markup.Escape(args)})");
        AnsiConsole.MarkupLine($"[magenta]tool[/] [bold]{invocation}[/]");
    }

    public void AddMcpToolCallEnd(bool success, string resultJson)
    {
        _history.AddMcpToolCallEnd(success, resultJson);
        _history.ScrollToBottom();
        string title = success ? "success" : "failed";
        AnsiConsole.MarkupLine($"[magenta]tool[/] {title}:");
        string formatted = TextFormatting.FormatAndTruncateToolResult(resultJson, 5, 80);
        foreach (var line in formatted.Split('\n'))
            AnsiConsole.MarkupLine($"[dim]{Markup.Escape(line)}[/]");
    }

    public void AddMcpToolCallImage(string resultJson)
    {
        string desc = ToolResultUtils.FormatImageInfo(resultJson);
        _history.AddMcpToolCallImage(desc);
        _history.ScrollToBottom();
        AnsiConsole.MarkupLine($"[magenta]tool[/] {desc}");
    }

    public void SetTaskRunning(bool running) =>
        _bottomPane.SetTaskRunning(running);

    /// <summary>
    /// Update the status indicator with the latest log line.
    /// Called by <see cref="LogBridge"/> when <c>LatestLog</c> events arrive.
    /// </summary>
    public void UpdateLatestLog(string line) =>
        _bottomPane.UpdateStatusText(line);

    public void ScrollUp(int lines) => _history.ScrollUp(lines);
    public void ScrollDown(int lines) => _history.ScrollDown(lines);
    public void ScrollPageUp(int height) => _history.ScrollPageUp(height);
    public void ScrollPageDown(int height) => _history.ScrollPageDown(height);
    public void ScrollToBottom() => _history.ScrollToBottom();

    public InputResult HandleKeyEvent(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Tab && !_bottomPane.IsCommandPopupVisible)
        {
            _focus = _focus == InputFocus.HistoryPane ? InputFocus.BottomPane : InputFocus.HistoryPane;
            _history.SetInputFocus(_focus == InputFocus.HistoryPane);
            _bottomPane.SetInputFocus(_focus == InputFocus.BottomPane);
            return InputResult.None;
        }

        if (_focus == InputFocus.HistoryPane)
        {
            _history.HandleKeyEvent(key);
            return InputResult.None;
        }

        return _bottomPane.HandleKeyEvent(key);
    }

    public IReadOnlyList<string> GetVisibleLines(int height) => _history.GetVisibleLines(height);

    public int CalculateRequiredHeight(int areaHeight) => _bottomPane.CalculateRequiredHeight(areaHeight);

    public bool HasActiveView => _bottomPane.HasActiveView;

    public void SetHistoryMetadata(string logId, int count) =>
        _bottomPane.SetHistoryMetadata(logId, count);

    public void OnHistoryEntryResponse(string logId, int offset, string? entry) =>
        _bottomPane.OnHistoryEntryResponse(logId, offset, entry);

    public ReviewDecision PushApprovalRequest(Event req) =>
        _bottomPane.PushApprovalRequest(req);

    public (int chatHeight, int bottomHeight) GetLayoutHeights(int totalHeight)
    {
        int desired = _bottomPane.CalculateRequiredHeight(totalHeight);
        int maxBottom = Math.Max(1, totalHeight - LayoutSpacing - 1);
        int bottomHeight = Math.Min(desired, maxBottom);
        int chatHeight = Math.Max(1, totalHeight - bottomHeight - LayoutSpacing);
        return (chatHeight, bottomHeight);
    }

    public void Render(int totalHeight)
    {
        var (chatHeight, bottomHeight) = GetLayoutHeights(totalHeight);
        foreach (var line in _history.GetVisibleLines(chatHeight))
            AnsiConsole.MarkupLine(line);
        AnsiConsole.MarkupLine(string.Empty);
        _bottomPane.Render(bottomHeight);
    }
}
