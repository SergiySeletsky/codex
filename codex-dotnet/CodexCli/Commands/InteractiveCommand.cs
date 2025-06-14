using System.CommandLine;
using Spectre.Console;
using CodexCli.Config;
using CodexCli.Util;
using System.Linq;
using SessionManager = CodexCli.Util.SessionManager;

namespace CodexCli.Commands;

public static class InteractiveCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var promptArg = new Argument<string?>("prompt", () => null, "Initial instructions");
        var imagesOpt = new Option<FileInfo[]>("--image") { AllowMultipleArgumentsPerToken = true };
        var modelOpt = new Option<string?>("--model");
        var profileOpt = new Option<string?>("--profile");
        var providerOpt = new Option<string?>("--model-provider");
        var fullAutoOpt = new Option<bool>("--full-auto", () => false);
        var approvalOpt = new Option<ApprovalMode?>("--ask-for-approval");
        var sandboxOpt = new Option<string[]>("-s") { AllowMultipleArgumentsPerToken = true };
        var colorOpt = new Option<ColorMode>("--color", () => ColorMode.Auto);
        var notifyOpt = new Option<string[]>("--notify") { AllowMultipleArgumentsPerToken = true };
        var overridesOpt = new Option<string[]>("-c") { AllowMultipleArgumentsPerToken = true };
        var skipGitOpt = new Option<bool>("--skip-git-repo-check", () => false);
        var cwdOpt = new Option<string?>(new[]{"--cwd","-C"});

        var cmd = new Command("interactive", "Run interactive TUI session");
        cmd.AddArgument(promptArg);
        cmd.AddOption(imagesOpt);
        cmd.AddOption(modelOpt);
        cmd.AddOption(profileOpt);
        cmd.AddOption(providerOpt);
        cmd.AddOption(fullAutoOpt);
        cmd.AddOption(approvalOpt);
        cmd.AddOption(sandboxOpt);
        cmd.AddOption(colorOpt);
        cmd.AddOption(notifyOpt);
        cmd.AddOption(overridesOpt);
        cmd.AddOption(skipGitOpt);
        cmd.AddOption(cwdOpt);

        var binder = new InteractiveBinder(promptArg, imagesOpt, modelOpt, profileOpt, providerOpt,
            fullAutoOpt, approvalOpt, sandboxOpt, colorOpt, skipGitOpt, cwdOpt, notifyOpt, overridesOpt);

        cmd.SetHandler(async (InteractiveOptions opts, string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);

            if (!opts.SkipGitRepoCheck && !GitUtils.IsInsideGitRepo(Environment.CurrentDirectory))
            {
                Console.Error.WriteLine("Not inside a git repo. Use --skip-git-repo-check to override.");
                return;
            }

            if (opts.Cwd != null) Environment.CurrentDirectory = opts.Cwd;

            if (opts.NotifyCommand.Length > 0)
                NotifyUtils.RunNotify(opts.NotifyCommand, "session_started");

            string? prompt = opts.Prompt;
            if (string.IsNullOrEmpty(prompt) || prompt == "-")
            {
                if (!Console.IsInputRedirected)
                {
                    Console.Error.WriteLine("No prompt provided. Provide as argument or pipe via stdin.");
                    return;
                }
                prompt = await Console.In.ReadToEndAsync();
            }

            var opts2 = opts with { Prompt = prompt };
            RunInteractive(opts2, cfg);
            if (opts.NotifyCommand.Length > 0)
                NotifyUtils.RunNotify(opts.NotifyCommand, "session_complete");
            await Task.CompletedTask;
        }, binder, configOption, cdOption);
        return cmd;
    }

    private static void RunInteractive(InteractiveOptions opts, AppConfig? cfg)
    {
        var sessionId = SessionManager.CreateSession();
        var history = new List<string>();
        AnsiConsole.MarkupLine("[green]Codex interactive mode[/]");
        AnsiConsole.MarkupLine("Type /help for commands");
        if (!string.IsNullOrEmpty(opts.Prompt))
        {
            history.Add(opts.Prompt);
            SessionManager.AddEntry(sessionId, opts.Prompt);
            AnsiConsole.MarkupLine($"Initial prompt: [blue]{opts.Prompt}[/]");
        }
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
            if (prompt.Equals("/reset", StringComparison.OrdinalIgnoreCase))
            {
                history.Clear();
                SessionManager.ClearHistory(sessionId);
                AnsiConsole.MarkupLine("History cleared");
                continue;
            }
            if (prompt.Equals("/help", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("Available commands: /history, /reset, /quit, /help, /log, /config, /save <file>");
                continue;
            }
            if (prompt.Equals("/log", StringComparison.OrdinalIgnoreCase))
            {
                var dir = cfg != null ? EnvUtils.GetLogDir(cfg) : Path.Combine(EnvUtils.FindCodexHome(), "log");
                AnsiConsole.MarkupLine($"Log dir: [blue]{dir}[/]");
                continue;
            }
            if (prompt.Equals("/config", StringComparison.OrdinalIgnoreCase))
            {
                if (cfg != null)
                {
                    AnsiConsole.MarkupLine($"Model: [blue]{cfg.Model}[/]");
                    var codexHome = cfg.CodexHome ?? EnvUtils.FindCodexHome();
                    AnsiConsole.MarkupLine($"CodexHome: [blue]{codexHome}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("No config loaded");
                }
                continue;
            }
            if (prompt.StartsWith("/save", StringComparison.OrdinalIgnoreCase))
            {
                var parts = prompt.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    AnsiConsole.MarkupLine("Usage: /save <file>");
                    continue;
                }
                var file = parts[1];
                File.WriteAllLines(file, history);
                AnsiConsole.MarkupLine($"Saved history to [green]{file}[/]");
                continue;
            }
            history.Add(prompt);
            SessionManager.AddEntry(sessionId, prompt);
            AnsiConsole.MarkupLine($"You typed: [blue]{prompt}[/]");
        }
        if (SessionManager.GetHistoryFile(sessionId) is { } path)
            AnsiConsole.MarkupLine($"History saved to [green]{path}[/]");
    }
}
