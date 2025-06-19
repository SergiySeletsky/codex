using System;
using System.Linq;
using System.Collections.Generic;
using Spectre.Console;
using CodexCli.Util;

namespace CodexCli.Interactive;

/// <summary>
/// Minimal chat composer with cursor-aware editing and command popup.
/// Mirrors codex-rs/tui/src/bottom_pane/chat_composer.rs (input logic and history hook done).
/// </summary>
public class ChatComposer
{
    private readonly ITextArea _textarea;
    private readonly AppEventSender _appEventTx;
    private readonly ChatComposerHistory _history = new();
    private readonly ConversationHistoryWidget? _conversationHistory;
    private CommandPopup? _commandPopup;

    public ChatComposer(bool hasFocus, AppEventSender sender, ConversationHistoryWidget? history = null)
    {
        _textarea = new BasicTextArea();
        _appEventTx = sender;
        _conversationHistory = history;
    }

    public void SetHistoryMetadata(string logId, int count) =>
        _history.SetMetadata(logId, count);

    public void SetInputFocus(bool hasFocus) { }

    public bool OnHistoryEntryResponse(string logId, int offset, string? entry)
        => _history.OnEntryResponse(logId, offset, entry, _textarea);

    public int CalculateRequiredHeight(int areaHeight)
    {
        int rows = _textarea.Lines.Count;
        int popup = _commandPopup?.CalculateRequiredHeight(areaHeight) ?? 0;
        return rows + popup;
    }

    public (InputResult Result, bool NeedsRedraw) HandleKeyEvent(ConsoleKeyInfo key)
    {
        var result = _commandPopup != null ? HandleWithPopup(key) : HandleWithoutPopup(key);
        SyncCommandPopup();
        return result;
    }

    private (InputResult,bool) HandleWithPopup(ConsoleKeyInfo key)
    {
        if (_commandPopup == null)
            return (InputResult.None, false);
        switch (key.Key)
        {
            case ConsoleKey.UpArrow:
                _commandPopup.MoveUp();
                return (InputResult.None, true);
            case ConsoleKey.DownArrow:
                _commandPopup.MoveDown();
                return (InputResult.None, true);
            case ConsoleKey.Tab:
                if (_commandPopup.SelectedCommand() is SlashCommand cmd)
                {
                    _textarea.SelectAll();
                    _textarea.Cut();
                    _textarea.InsertString($"/{cmd.Command()} ");
                }
                return (InputResult.None, true);
            case ConsoleKey.Enter when key.Modifiers == 0:
                if (_commandPopup.SelectedCommand() is SlashCommand cmd2)
                {
                    _textarea.SelectAll();
                    _textarea.Cut();
                    return (InputResult.Submitted($"/{cmd2.Command()}"), true);
                }
                break;
        }
        return HandleWithoutPopup(key);
    }

    private (InputResult,bool) HandleWithoutPopup(ConsoleKeyInfo key)
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
        if (key.KeyChar != '\0' && key.Key != ConsoleKey.Enter &&
            key.Key != ConsoleKey.Backspace)
        {
            _textarea.InsertChar(key.KeyChar);
            return (InputResult.None, true);
        }
        if (key.Key == ConsoleKey.Backspace)
        {
            _textarea.DeleteCharBeforeCursor();
            return (InputResult.None, true);
        }
        if (key.Key == ConsoleKey.LeftArrow)
        {
            var (row,col) = _textarea.Cursor;
            _textarea.MoveCursor(row, Math.Max(0, col - 1));
            return (InputResult.None, true);
        }
        if (key.Key == ConsoleKey.RightArrow)
        {
            var (row,col) = _textarea.Cursor;
            var line = _textarea.Lines[row];
            _textarea.MoveCursor(row, Math.Min(line.Length, col + 1));
            return (InputResult.None, true);
        }
        if (key.Key == ConsoleKey.Enter && key.Modifiers == 0)
        {
            var text = string.Join("\n", _textarea.Lines);
            _textarea.SelectAll();
            _textarea.Cut();
            if (text.Length == 0)
                return (InputResult.None, true);
            _history.RecordLocalSubmission(text);
            if (_conversationHistory != null)
            {
                var clean = AnsiEscape.StripAnsi(text);
                _conversationHistory.AddUserMessage(clean);
                _conversationHistory.ScrollToBottom();
            }
            return (InputResult.Submitted(text), true);
        }
        return (InputResult.None, false);
    }

    private void SyncCommandPopup()
    {
        var firstLine = _textarea.Lines.FirstOrDefault() ?? string.Empty;
        if (firstLine.StartsWith('/'))
        {
            _commandPopup ??= new CommandPopup();
            _commandPopup.OnComposerTextChange(firstLine);
        }
        else
        {
            _commandPopup = null;
        }
    }

    public bool IsCommandPopupVisible => _commandPopup != null;

    public void Render(int areaHeight)
    {
        if (_commandPopup != null)
        {
            _commandPopup.Render();
        }
        var text = string.Join("\n", _textarea.Lines);
        if (text.Length > 0)
            Spectre.Console.AnsiConsole.MarkupLine($"> {Spectre.Console.Markup.Escape(text)}");
    }

    private class BasicTextArea : ITextArea
    {
        private List<string> _lines = new() { string.Empty };
        private int _row;
        private int _col;

        public IReadOnlyList<string> Lines => _lines;
        public (int Row, int Col) Cursor => (_row, _col);

        public void SelectAll() { /* no-op */ }

        public void Cut()
        {
            _lines = new() { string.Empty };
            _row = 0;
            _col = 0;
        }

        public void InsertString(string text)
        {
            _lines = new(text.Split('\n'));
            _row = _lines.Count - 1;
            _col = _lines[^1].Length;
        }

        public void MoveCursor(int row, int col)
        {
            _row = Math.Clamp(row, 0, _lines.Count - 1);
            _col = Math.Clamp(col, 0, _lines[_row].Length);
        }

        public void InsertChar(char ch)
        {
            var line = _lines[_row];
            if (_col >= line.Length)
                line += ch;
            else
                line = line.Insert(_col, ch.ToString());
            _lines[_row] = line;
            _col++;
        }

        public void DeleteCharBeforeCursor()
        {
            if (_col > 0)
            {
                var line = _lines[_row];
                line = line.Remove(_col - 1, 1);
                _lines[_row] = line;
                _col--;
            }
            else if (_row > 0)
            {
                _col = _lines[_row - 1].Length;
                _lines[_row - 1] += _lines[_row];
                _lines.RemoveAt(_row);
                _row--;
            }
        }
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
