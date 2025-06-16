using Spectre.Console;
using CodexCli.Util;

namespace CodexTui;

/// <summary>
/// Very minimal chat widget placeholder.
/// Mirrors codex-rs/tui/src/chatwidget.rs (in progress).
/// </summary>
internal class ChatWidget
{
    private readonly List<string> _history = new();

    public void AddUserMessage(string text)
    {
        var clean = AnsiEscape.StripAnsi(text);
        _history.Add($"You: {clean}");
        AnsiConsole.MarkupLine($"[bold cyan]You:[/] {clean}");
    }

    public void AddAgentMessage(string text)
    {
        var clean = AnsiEscape.StripAnsi(text);
        _history.Add($"Codex: {clean}");
        AnsiConsole.MarkupLine($"[bold green]Codex:[/] {clean}");
    }

    public void Render()
    {
        foreach (var line in _history)
            AnsiConsole.MarkupLine(line);
    }
}
