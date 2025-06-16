using Spectre.Console;

namespace CodexTui;

/// <summary>
/// Warning screen when running outside a git repository.
/// Mirrors codex-rs/tui/src/git_warning_screen.rs (done).
/// </summary>
internal static class GitWarningScreen
{
    public static bool ShowAndAsk()
    {
        AnsiConsole.MarkupLine("We recommend running codex inside a git repository. This helps ensure that changes can be tracked and easily rolled back if necessary.");
        AnsiConsole.Markup("Do you wish to proceed? [yellow](y/n)[/] ");
        var key = Console.ReadKey(true);
        return key.KeyChar == 'y' || key.KeyChar == 'Y';
    }
}
