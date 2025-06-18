using System.Collections.Generic;
using Spectre.Console;
using CodexCli.Util;
using CodexCli.Protocol;

namespace CodexCli.Interactive;

/// <summary>
/// Very simple scrollable history log with basic formatting helpers.
/// Mirrors codex-rs/tui/src/conversation_history_widget.rs (scrolling,
/// message formatting, history entry helpers and patch diff summary done, rendering in progress).
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

    public void AddBackgroundEvent(string text)
    {
        var clean = Util.AnsiEscape.StripAnsi(text);
        Add($"[gray]{Markup.Escape(clean)}[/]");
    }

    public void AddError(string text)
    {
        var clean = Util.AnsiEscape.StripAnsi(text);
        Add($"[red]ERROR: {Markup.Escape(clean)}[/]");
    }

    public void AddExecCommand(string command)
    {
        Add($"[magenta]exec[/] {Markup.Escape(command)}");
    }

    public void AddExecResult(int exitCode)
    {
        var status = exitCode == 0 ? "succeeded" : $"exited {exitCode}";
        Add($"[magenta]exec[/] {status}");
    }

    public void AddPatchApplyBegin(bool autoApproved, IReadOnlyDictionary<string,FileChange>? changes = null)
    {
        if (changes == null)
        {
            Add($"[magenta]apply_patch[/] auto_approved={autoApproved}");
            return;
        }

        Add("[magenta]applying patch[/]");
        foreach (var line in FormatPatchLines(changes))
            Add(line);
    }

    public void AddPatchApplyEnd(bool success)
    {
        var status = success ? "succeeded" : "failed";
        Add($"[magenta]apply_patch[/] {status}");
    }

    public void AddHistoryEntry(int offset, string text)
    {
        var clean = Util.AnsiEscape.StripAnsi(text);
        Add($"[dim]history {offset}: {Markup.Escape(clean)}[/]");
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
