using System.Collections.Generic;
using Spectre.Console;
using CodexCli.Util;
using CodexCli.Protocol;
using CodexCli.Config;

namespace CodexCli.Interactive;

/// <summary>
/// Very simple scrollable history log with basic formatting helpers.
/// Mirrors codex-rs/tui/src/conversation_history_widget.rs (scrolling,
/// message formatting with markdown, text block storage, history entry helpers,
/// exec/patch events, mcp tool calls with formatted results, diff summary and
/// HistoryCell image placeholders done.
/// </summary>
public class ConversationHistoryWidget
{
    private class Entry
    {
        public HistoryCell Cell;
        public int LineCount;
    }

    private readonly List<Entry> _entries = new();
    private int _scrollOffset = 0; // lines scrolled back from bottom
    private bool _hasInputFocus;
    private readonly UriBasedFileOpener _fileOpener;
    private readonly string _cwd;
    private const int Width = 80;

    public ConversationHistoryWidget(UriBasedFileOpener opener = UriBasedFileOpener.None, string? cwd = null)
    {
        _fileOpener = opener;
        _cwd = cwd ?? Environment.CurrentDirectory;
    }

    public void AddUserMessage(string text)
    {
        var lines = new List<string>();
        MarkdownUtils.AppendMarkdown(Util.AnsiEscape.StripAnsi(text), lines, _fileOpener, _cwd);
        if (lines.Count == 0)
            lines.Add(string.Empty);
        lines[0] = $"[bold cyan]You:[/] {Markup.Escape(lines[0])}";
        AddLines(lines, HistoryCell.CellType.User);
    }

    public void AddUserImage(string path)
    {
        string desc = ToolResultUtils.FormatImageInfoFromFile(path);
        AddLines(new[] { $"[bold cyan]You:[/] {desc}" }, HistoryCell.CellType.UserImage);
    }

    public void AddAgentMessage(string text)
    {
        var lines = new List<string>();
        MarkdownUtils.AppendMarkdown(Util.AnsiEscape.StripAnsi(text), lines, _fileOpener, _cwd);
        if (lines.Count == 0)
            lines.Add(string.Empty);
        lines[0] = $"[bold green]Codex:[/] {Markup.Escape(lines[0])}";
        AddLines(lines, HistoryCell.CellType.Agent);
    }

    public void Clear()
    {
        _entries.Clear();
        _scrollOffset = 0;
    }

    public void AddSystemMessage(string text)
    {
        var lines = new List<string>();
        MarkdownUtils.AppendMarkdown(Util.AnsiEscape.StripAnsi(text), lines, _fileOpener, _cwd);
        if (lines.Count == 0)
            lines.Add(string.Empty);
        lines[0] = $"[bold yellow]System:[/] {Markup.Escape(lines[0])}";
        AddLines(lines, HistoryCell.CellType.System);
    }

    public void AddAgentReasoning(string text)
    {
        var lines = new List<string>();
        MarkdownUtils.AppendMarkdown(Util.AnsiEscape.StripAnsi(text), lines, _fileOpener, _cwd);
        for (int i = 0; i < lines.Count; i++)
            lines[i] = $"[italic]{Markup.Escape(lines[i])}[/]";
        AddLines(lines, HistoryCell.CellType.Reasoning);
    }

    public void AddBackgroundEvent(string text)
    {
        var clean = Util.AnsiEscape.StripAnsi(text);
        AddLines(new[]{$"[gray]{Markup.Escape(clean)}[/]"}, HistoryCell.CellType.Background);
    }

    public void AddError(string text)
    {
        var clean = Util.AnsiEscape.StripAnsi(text);
        AddLines(new[]{$"[red]ERROR: {Markup.Escape(clean)}[/]"}, HistoryCell.CellType.Error);
    }

    public void AddExecCommand(string command)
    {
        AddLines(new[]{$"[magenta]exec[/] {Markup.Escape(command)}"}, HistoryCell.CellType.ExecCommand);
    }

    public void AddExecResult(int exitCode)
    {
        var status = exitCode == 0 ? "succeeded" : $"exited {exitCode}";
        AddLines(new[]{$"[magenta]exec[/] {status}"}, HistoryCell.CellType.ExecResult);
    }

    public void AddPatchApplyBegin(bool autoApproved, IReadOnlyDictionary<string,FileChange>? changes = null)
    {
        if (changes == null)
        {
            AddLines(new[]{$"[magenta]apply_patch[/] auto_approved={autoApproved}"}, HistoryCell.CellType.PatchBegin);
            return;
        }

        var all = new List<string> { "[magenta]applying patch[/]" };
        all.AddRange(FormatPatchLines(changes));
        AddLines(all, HistoryCell.CellType.PatchBegin);
    }

    public void AddPatchApplyEnd(bool success)
    {
        var status = success ? "succeeded" : "failed";
        AddLines(new[]{$"[magenta]apply_patch[/] {status}"}, HistoryCell.CellType.PatchEnd);
    }

    private const int ToolCallMaxLines = 5;

    public void AddMcpToolCallBegin(string server, string tool, string? args)
    {
        string invocation = $"{server}.{tool}" + (string.IsNullOrEmpty(args) ? "()" : $"({Markup.Escape(args)})");
        AddLines(new[] { $"[magenta]tool[/] [bold]{invocation}[/]" }, HistoryCell.CellType.ToolBegin);
    }

    public void AddMcpToolCallEnd(bool success, string resultJson)
    {
        string title = success ? "success" : "failed";
        var lines = new List<string> { $"[magenta]tool[/] {title}:" };
        string formatted = TextFormatting.FormatAndTruncateToolResult(resultJson, ToolCallMaxLines, 80);
        foreach (var line in formatted.Split('\n'))
            lines.Add($"[dim]{Markup.Escape(line)}[/]");
        AddLines(lines, HistoryCell.CellType.ToolEnd);
    }

    public void AddMcpToolCallImage(string description)
    {
        AddLines(new[]{"[magenta]tool[/] " + description}, HistoryCell.CellType.ToolImage);
    }

    public void AddHistoryEntry(int offset, string text)
    {
        var clean = Util.AnsiEscape.StripAnsi(text);
        AddLines(new[] { $"[dim]history {offset}: {Markup.Escape(clean)}[/]" }, HistoryCell.CellType.HistoryEntry);
    }

    private void AddLines(IEnumerable<string> lines, HistoryCell.CellType type)
    {
        var cell = new HistoryCell(type, lines);
        _entries.Add(new Entry { Cell = cell, LineCount = cell.Height(Width) });
    }

    public void ScrollUp(int lines)
    {
        _scrollOffset = Math.Min(_scrollOffset + lines, Math.Max(0, TotalHeight() - 1));
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
        int totalHeight = TotalHeight();
        int maxOffset = Math.Max(0, totalHeight - height);
        _scrollOffset = Math.Min(_scrollOffset, maxOffset);
        int start = Math.Max(0, totalHeight - height - _scrollOffset);

        var visible = new List<string>();
        int currentLine = 0;
        foreach (var entry in _entries)
        {
            if (currentLine + entry.LineCount <= start)
            {
                currentLine += entry.LineCount;
                continue;
            }

            int first = Math.Max(0, start - currentLine);
            int remainingHeight = height - visible.Count;
            if (remainingHeight <= 0) break;
            var lines = entry.Cell.RenderWindow(first, remainingHeight, Width);
            visible.AddRange(lines);
            currentLine += entry.LineCount;
            if (visible.Count >= height) break;
        }

        return visible;
    }

    private int TotalHeight()
    {
        int total = 0;
        foreach (var entry in _entries)
            total += entry.LineCount;
        return total;
    }

    private static List<string> CreateDiffSummary(IReadOnlyDictionary<string,FileChange> changes)
    {
        var lines = new List<string>();
        foreach (var kv in changes)
        {
            var path = kv.Key;
            switch (kv.Value)
            {
                case AddFileChange add:
                    int added = add.Content.Split('\n').Length;
                    if (add.Content.EndsWith("\n")) added--;
                    lines.Add($"A {path} (+{added})");
                    break;
                case DeleteFileChange:
                    lines.Add($"D {path}");
                    break;
                case UpdateFileChange upd:
                    if (upd.MovePath != null)
                        lines.Add($"R {path} -> {upd.MovePath}");
                    else
                        lines.Add($"M {path}");
                    lines.AddRange(upd.UnifiedDiff.Split('\n'));
                    break;
            }
        }
        return lines;
    }

    private static string FormatPatchLine(string line)
    {
        if (line.StartsWith("+"))
            return $"[green]{Markup.Escape(line)}[/]";
        if (line.StartsWith("-"))
            return $"[red]{Markup.Escape(line)}[/]";
        if (line.Length > 2 && line[1] == ' ')
        {
            char kind = line[0];
            string rest = line.Substring(2);
            string color = kind switch
            {
                'A' => "green",
                'D' => "red",
                'M' => "yellow",
                'R' => "cyan",
                'C' => "cyan",
                _ => "white"
            };
            return $"[{color} bold]{kind}[/] {Markup.Escape(rest)}";
        }
        return Markup.Escape(line);
    }

    internal static IEnumerable<string> FormatPatchLines(IReadOnlyDictionary<string,FileChange> changes)
    {
        foreach (var l in CreateDiffSummary(changes))
            yield return FormatPatchLine(l);
    }
}
