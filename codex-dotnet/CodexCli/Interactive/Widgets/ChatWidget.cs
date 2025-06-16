using Spectre.Console;
using CodexCli.Util;

namespace CodexCli.Interactive;

/// <summary>
/// Very minimal chat widget placeholder.
/// Mirrors codex-rs/tui/src/chatwidget.rs (in progress).
/// </summary>
public class ChatWidget
{
    private readonly ConversationHistoryWidget _history = new();

    public void AddUserMessage(string text)
    {
        var clean = AnsiEscape.StripAnsi(text);
        _history.Add($"[bold cyan]You:[/] {clean}");
        AnsiConsole.MarkupLine($"[bold cyan]You:[/] {clean}");
    }

    public void AddAgentMessage(string text)
    {
        var clean = AnsiEscape.StripAnsi(text);
        _history.Add($"[bold green]Codex:[/] {clean}");
        AnsiConsole.MarkupLine($"[bold green]Codex:[/] {clean}");
    }

    public void AddSystemMessage(string text)
    {
        var clean = AnsiEscape.StripAnsi(text);
        _history.Add($"[bold yellow]System:[/] {clean}");
        AnsiConsole.MarkupLine($"[bold yellow]System:[/] {clean}");
    }

    public void ScrollUp(int lines) => _history.ScrollUp(lines);
    public void ScrollDown(int lines) => _history.ScrollDown(lines);

    public IReadOnlyList<string> GetVisibleLines(int height) => _history.GetVisibleLines(height);

    public void Render(int height = 10)
    {
        foreach (var line in _history.GetVisibleLines(height))
            AnsiConsole.MarkupLine(line);
    }
}
