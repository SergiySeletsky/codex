using System;
using Spectre.Console;
using CodexCli.Util;
using CodexCli.Protocol;

namespace CodexCli.Interactive;

/// <summary>
/// Chat widget managing conversation history and bottom pane input.
/// Mirrors codex-rs/tui/src/chatwidget.rs
/// (status indicator, log bridge and agent reasoning done).
/// </summary>
public class ChatWidget
{
    private readonly ConversationHistoryWidget _history = new();
    private readonly BottomPane _bottomPane;
    private InputFocus _focus = InputFocus.BottomPane;

    private enum InputFocus { HistoryPane, BottomPane }

    public ChatWidget() : this(new AppEventSender(_ => { })) {}

    public ChatWidget(AppEventSender sender)
    {
        _bottomPane = new BottomPane(sender, hasInputFocus: true, _history);
    }

    public void AddUserMessage(string text)
    {
        _history.AddUserMessage(text);
        _history.ScrollToBottom();
        var clean = AnsiEscape.StripAnsi(text);
        AnsiConsole.MarkupLine($"[bold cyan]You:[/] {clean}");
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

    public void AddAgentReasoning(string text)
    {
        _history.AddAgentReasoning(text);
        _history.ScrollToBottom();
        var clean = AnsiEscape.StripAnsi(text);
        AnsiConsole.MarkupLine($"[italic]{Markup.Escape(clean)}[/]");
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

    public void Render(int totalHeight)
    {
        int bottomHeight = Math.Max(1, _bottomPane.CalculateRequiredHeight(totalHeight / 2));
        int chatHeight = Math.Max(1, totalHeight - bottomHeight);
        foreach (var line in _history.GetVisibleLines(chatHeight))
            AnsiConsole.MarkupLine(line);
        _bottomPane.Render(bottomHeight);
    }
}
