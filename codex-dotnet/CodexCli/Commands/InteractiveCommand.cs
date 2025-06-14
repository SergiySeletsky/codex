using System.CommandLine;
using Spectre.Console;
using CodexCli.Config;

namespace CodexCli.Commands;

public static class InteractiveCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var cmd = new Command("interactive", "Run interactive TUI session");
        cmd.SetHandler(async (string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);

            if (cfg?.NotifyCommand is { } notify)
            {
                try { System.Diagnostics.Process.Start(notify, "session_started"); } catch { }
            }
            RunInteractive();
            await Task.CompletedTask;
        }, configOption, cdOption);
        return cmd;
    }

    private static void RunInteractive()
    {
        AnsiConsole.MarkupLine("[green]Codex interactive mode[/]");
        while (true)
        {
            var prompt = AnsiConsole.Ask<string>("Enter command (or 'quit'): ");
            if (prompt.Equals("quit", StringComparison.OrdinalIgnoreCase))
                break;
            AnsiConsole.MarkupLine($"You typed: [blue]{prompt}[/]");
        }
    }
}
