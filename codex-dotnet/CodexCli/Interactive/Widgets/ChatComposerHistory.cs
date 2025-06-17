using System.Collections.Generic;
using CodexCli.Protocol;

namespace CodexCli.Interactive;

/// <summary>
/// Manages shell-style history navigation for the chat composer.
/// Mirrors codex-rs/tui/src/bottom_pane/chat_composer_history.rs (done).
/// </summary>
public class ChatComposerHistory
{
    private string? _historyLogId;
    private int _historyEntryCount;
    private readonly List<string> _localHistory = new();
    private readonly Dictionary<int, string> _fetchedHistory = new();
    private int? _historyCursor;
    private string? _lastHistoryText;

    public void SetMetadata(string logId, int entryCount)
    {
        _historyLogId = logId;
        _historyEntryCount = entryCount;
        _fetchedHistory.Clear();
        _localHistory.Clear();
        _historyCursor = null;
        _lastHistoryText = null;
    }

    public void RecordLocalSubmission(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            _localHistory.Add(text);
            _historyCursor = null;
            _lastHistoryText = null;
        }
    }

    public bool ShouldHandleNavigation(ITextArea textarea)
    {
        if (_historyEntryCount == 0 && _localHistory.Count == 0)
            return false;

        var lines = textarea.Lines;
        if (lines.Count == 1 && lines[0].Length == 0)
            return true;

        var (row, col) = textarea.Cursor;
        if (row != 0 || col != 0)
            return false;

        return _lastHistoryText != null && string.Join("\n", lines) == _lastHistoryText;
    }

    public bool NavigateUp(ITextArea textarea, AppEventSender sender)
    {
        int total = _historyEntryCount + _localHistory.Count;
        if (total == 0)
            return false;

        int next = _historyCursor switch
        {
            null => total - 1,
            0 => 0,
            int n => n - 1
        };

        _historyCursor = next;
        PopulateHistoryAtIndex(next, textarea, sender);
        return true;
    }

    public bool NavigateDown(ITextArea textarea, AppEventSender sender)
    {
        int total = _historyEntryCount + _localHistory.Count;
        if (total == 0)
            return false;

        int? next = _historyCursor switch
        {
            null => null,
            int n when n + 1 >= total => null,
            int n => n + 1
        };

        if (next is int idx)
        {
            _historyCursor = idx;
            PopulateHistoryAtIndex(idx, textarea, sender);
        }
        else
        {
            _historyCursor = null;
            _lastHistoryText = null;
            ReplaceTextareaContent(textarea, string.Empty);
        }
        return true;
    }

    public bool OnEntryResponse(string logId, int offset, string? entry, ITextArea textarea)
    {
        if (_historyLogId != logId || entry == null)
            return false;
        _fetchedHistory[offset] = entry;
        if (_historyCursor == offset)
        {
            ReplaceTextareaContent(textarea, entry);
            return true;
        }
        return false;
    }

    private void PopulateHistoryAtIndex(int globalIdx, ITextArea textarea, AppEventSender sender)
    {
        if (globalIdx >= _historyEntryCount)
        {
            if (globalIdx - _historyEntryCount < _localHistory.Count)
            {
                ReplaceTextareaContent(textarea, _localHistory[globalIdx - _historyEntryCount]);
            }
        }
        else if (_fetchedHistory.TryGetValue(globalIdx, out var text))
        {
            ReplaceTextareaContent(textarea, text);
        }
        else if (_historyLogId != null)
        {
            sender.Send(new GetHistoryEntryRequestEvent(string.Empty, _historyLogId, globalIdx));
        }
    }

    private void ReplaceTextareaContent(ITextArea textarea, string text)
    {
        textarea.SelectAll();
        textarea.Cut();
        textarea.InsertString(text);
        textarea.MoveCursor(0, 0);
        _lastHistoryText = text;
    }
}

/// <summary>
/// Minimal textarea interface required by ChatComposerHistory.
/// </summary>
public interface ITextArea
{
    IReadOnlyList<string> Lines { get; }
    (int Row, int Col) Cursor { get; }
    void SelectAll();
    void Cut();
    void InsertString(string text);
    void InsertChar(char ch);
    void DeleteCharBeforeCursor();
    void MoveCursor(int row, int col);
}

