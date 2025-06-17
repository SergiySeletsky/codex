using System;
using System.Collections.Generic;
using System.Linq;

namespace CodexCli.Interactive;

/// <summary>
/// Slash command enumeration used by the chat composer.
/// Mirrors codex-rs/tui/src/slash_command.rs (in progress).
/// </summary>
public enum SlashCommand
{
    New,
    ToggleMouseMode,
    Quit,
}

public static class SlashCommandExtensions
{
    public static string Description(this SlashCommand cmd) => cmd switch
    {
        SlashCommand.New => "Start a new chat.",
        SlashCommand.ToggleMouseMode => "Toggle mouse mode (enable for scrolling, disable for text selection)",
        SlashCommand.Quit => "Exit the application.",
        _ => string.Empty,
    };

    public static string Command(this SlashCommand cmd) => cmd.ToString().ToLowerInvariant().Replace('_', '-');
}

public static class SlashCommandBuiltIns
{
    public static readonly Dictionary<string, SlashCommand> All =
        Enum.GetValues<SlashCommand>().ToDictionary(c => c.Command());
}
