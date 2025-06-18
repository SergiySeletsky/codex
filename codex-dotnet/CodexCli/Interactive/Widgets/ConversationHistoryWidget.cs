using System.Collections.Generic;
using Spectre.Console;
using CodexCli.Util;

namespace CodexCli.Interactive;

/// <summary>
/// Very simple scrollable history log with basic formatting helpers.
/// Mirrors codex-rs/tui/src/conversation_history_widget.rs (scrolling done,
/// rendering in progress).
/// </summary>
public class ConversationHistoryWidget
{
    private readonly List<string> _entries = new();
    private int _scrollOffset = 0; // lines scrolled back from bottom
    private bool _hasInputFocus;

    public void AddUserMessage(string text)
    {
        var clean = Util.AnsiEscape.StripAnsi(text);
        Add($"[bold cyan]You:[/] {clean}");
    }

    public void AddAgentMessage(string text)
    {
        var clean = Util.AnsiEscape.StripAnsi(text);
        Add($"[bold green]Codex:[/] {clean}");
    }

    public void AddSystemMessage(string text)
    {
        var clean = Util.AnsiEscape.StripAnsi(text);
        Add($"[bold yellow]System:[/] {clean}");
    }

    public void AddAgentReasoning(string text)
    {
        var clean = Util.AnsiEscape.StripAnsi(text);
        Add($"[italic]{Markup.Escape(clean)}[/]");
    }

    public void Add(string text)
    {
        _entries.Add(text);
    }

    public void ScrollUp(int lines)
    {
        _scrollOffset = Math.Min(_scrollOffset + lines, Math.Max(0, _entries.Count - 1));
    }

    public void ScrollDown(int lines)
    {
        _scrollOffset = Math.Max(0, _scrollOffset - lines);
    }

    public void ScrollPageUp(int height)
    {
        ScrollUp(height);
    }

    public void ScrollPageDown(int height)
    {
        ScrollDown(height);
    }

    public void ScrollToBottom()
    {
        _scrollOffset = 0;
    }

    public void SetInputFocus(bool focus) => _hasInputFocus = focus;

    public bool HandleKeyEvent(ConsoleKeyInfo key)
    {
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
            case ConsoleKey.K:
                ScrollUp(1);
                return true;
            case ConsoleKey.DownArrow:
            case ConsoleKey.J:
                ScrollDown(1);
                return true;
            case ConsoleKey.PageUp:
            case ConsoleKey.B:
                ScrollPageUp(10);
                return true;
            case ConsoleKey.PageDown:
            case ConsoleKey.Spacebar:
                ScrollPageDown(10);
                return true;
        }
        return false;
    }

    public IReadOnlyList<string> GetVisibleLines(int height)
    {
        if (height <= 0)
            return Array.Empty<string>();
        int maxOffset = Math.Max(0, _entries.Count - height);
        _scrollOffset = Math.Min(_scrollOffset, maxOffset);
        int start = Math.Max(0, _entries.Count - height - _scrollOffset);
        int count = Math.Min(height, _entries.Count - start);
        return _entries.GetRange(start, count);
    }
}
