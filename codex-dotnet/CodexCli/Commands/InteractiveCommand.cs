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
        var history = new List<string>();
        AnsiConsole.MarkupLine("[green]Codex interactive mode[/]");
        AnsiConsole.MarkupLine("Type /help for commands");
        while (true)
        {
            var prompt = AnsiConsole.Ask<string>("cmd> ");
            if (prompt.Equals("/quit", StringComparison.OrdinalIgnoreCase))
                break;
            if (prompt.Equals("/history", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var item in history)
                    AnsiConsole.MarkupLine($"[blue]{item}[/]");
                continue;
            }
            if (prompt.Equals("/help", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("Available commands: /history, /quit, /help");
                continue;
            }
            history.Add(prompt);
            AnsiConsole.MarkupLine($"You typed: [blue]{prompt}[/]");
        }
    }
}
