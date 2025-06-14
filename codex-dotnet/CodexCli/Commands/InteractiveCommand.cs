using System.CommandLine;
using Spectre.Console;
using CodexCli.Config;
using CodexCli.Util;
using System.Linq;

namespace CodexCli.Commands;

public static class InteractiveCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var promptArg = new Argument<string?>("prompt", () => null, "Initial instructions");
        var imagesOpt = new Option<FileInfo[]>("--image") { AllowMultipleArgumentsPerToken = true };
        var modelOpt = new Option<string?>("--model");
        var profileOpt = new Option<string?>("--profile");
        var fullAutoOpt = new Option<bool>("--full-auto", () => false);
        var approvalOpt = new Option<ApprovalMode?>("--ask-for-approval");
        var sandboxOpt = new Option<string[]>("-s") { AllowMultipleArgumentsPerToken = true };
        var skipGitOpt = new Option<bool>("--skip-git-repo-check", () => false);
        var cwdOpt = new Option<string?>(new[]{"--cwd","-C"});

        var cmd = new Command("interactive", "Run interactive TUI session");
        cmd.AddArgument(promptArg);
        cmd.AddOption(imagesOpt);
        cmd.AddOption(modelOpt);
        cmd.AddOption(profileOpt);
        cmd.AddOption(fullAutoOpt);
        cmd.AddOption(approvalOpt);
        cmd.AddOption(sandboxOpt);
        cmd.AddOption(skipGitOpt);
        cmd.AddOption(cwdOpt);

        var binder = new InteractiveBinder(promptArg, imagesOpt, modelOpt, profileOpt,
            fullAutoOpt, approvalOpt, sandboxOpt, skipGitOpt, cwdOpt);

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

            if (cfg?.NotifyCommand is { Length: >0 } notify)
            {
                try { System.Diagnostics.Process.Start(notify[0], string.Join(' ', notify.Skip(1).Concat(new[]{"session_started"}))); } catch { }
            }

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
            RunInteractive(opts2);
            await Task.CompletedTask;
        }, binder, configOption, cdOption);
        return cmd;
    }

    private static void RunInteractive(InteractiveOptions opts)
    {
        var history = new List<string>();
        AnsiConsole.MarkupLine("[green]Codex interactive mode[/]");
        AnsiConsole.MarkupLine("Type /help for commands");
        if (!string.IsNullOrEmpty(opts.Prompt))
        {
            history.Add(opts.Prompt);
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
