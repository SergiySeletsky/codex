using System;

namespace CodexCli.Interactive;

/// <summary>
/// Minimal chat composer handling basic history navigation.
/// Mirrors codex-rs/tui/src/bottom_pane/chat_composer.rs (in progress).
/// </summary>
internal class ChatComposer
{
    private readonly ITextArea _textarea;
    private readonly AppEventSender _appEventTx;
    private readonly ChatComposerHistory _history = new();

    public ChatComposer(bool hasFocus, AppEventSender sender)
    {
        _textarea = new SimpleTextArea();
        _appEventTx = sender;
    }

    public void SetHistoryMetadata(string logId, int count) =>
        _history.SetMetadata(logId, count);

    public void SetInputFocus(bool hasFocus) { }

    public bool OnHistoryEntryResponse(string logId, int offset, string? entry)
        => _history.OnEntryResponse(logId, offset, entry, _textarea);

    public int CalculateRequiredHeight(int areaHeight) => 1;

    public (InputResult Result, bool NeedsRedraw) HandleKeyEvent(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.UpArrow)
        {
            if (_history.ShouldHandleNavigation(_textarea))
            {
                var consumed = _history.NavigateUp(_textarea, _appEventTx);
                return (InputResult.None, consumed);
            }
            return (InputResult.None, false);
        }
        if (key.Key == ConsoleKey.DownArrow)
        {
            if (_history.ShouldHandleNavigation(_textarea))
            {
                var consumed = _history.NavigateDown(_textarea, _appEventTx);
                return (InputResult.None, consumed);
            }
            return (InputResult.None, false);
        }
        if (key.Key == ConsoleKey.Enter && key.Modifiers == 0)
        {
            var text = string.Join("\n", _textarea.Lines);
            _textarea.SelectAll();
            _textarea.Cut();
            if (text.Length == 0)
                return (InputResult.None, true);
            _history.RecordLocalSubmission(text);
            return (InputResult.Submitted(text), true);
        }
        return (InputResult.None, false);
    }

    private class SimpleTextArea : ITextArea
    {
        private List<string> _lines = new() { string.Empty };
        private int _row;
        private int _col;
        public IReadOnlyList<string> Lines => _lines;
        public (int Row, int Col) Cursor => (_row, _col);
        public void SelectAll() { }
        public void Cut() { _lines = new() { string.Empty }; }
        public void InsertString(string text) => _lines = new(text.Split('\n'));
        public void MoveCursor(int row, int col) { _row = row; _col = col; }
    }
}

public readonly struct InputResult
{
    public string? SubmittedText { get; }
    public bool IsSubmitted => SubmittedText != null;
    public InputResult(string? text) => SubmittedText = text;
    public static InputResult None { get; } = new(null);
    public static InputResult Submitted(string text) => new(text);
}
