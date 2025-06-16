using Spectre.Console;

namespace CodexTui;

/// <summary>
/// Simple login screen shown when no API key is configured.
/// Mirrors codex-rs/tui/src/login_screen.rs (done).
/// </summary>
internal static class LoginScreen
{
    public static void Show()
    {
        AnsiConsole.MarkupLine("Login using `codex login` and then run this command again. 'q' to quit.");
        while (Console.ReadKey(true).KeyChar != 'q')
        {
        }
    }
}
