using System.Collections.Generic;

namespace CodexCli.Interactive;

/// <summary>
/// Very simple scrollable history log. Not feature complete.
/// Mirrors codex-rs/tui/src/conversation_history_widget.rs (in progress).
/// </summary>
public class ConversationHistoryWidget
{
    private readonly List<string> _entries = new();
    private int _scrollOffset = 0; // lines scrolled back from bottom

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
