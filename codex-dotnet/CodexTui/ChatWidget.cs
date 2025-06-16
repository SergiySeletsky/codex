using Spectre.Console;

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
        _history.Add($"You: {text}");
    }

    public void AddAgentMessage(string text)
    {
        _history.Add($"Codex: {text}");
    }

    public void Render()
    {
        foreach (var line in _history)
            AnsiConsole.MarkupLine(line);
    }
}
