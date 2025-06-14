using System.CommandLine;
using Spectre.Console;
using CodexCli.Config;
using CodexCli.Util;
using System.Linq;
using SessionManager = CodexCli.Util.SessionManager;
using System.IO;

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
        var effortOpt = new Option<ReasoningEffort?>("--reasoning-effort");
        var summaryOpt = new Option<ReasoningSummary?>("--reasoning-summary");
        var instrOpt = new Option<string?>("--instructions");
        var hideReasonOpt = new Option<bool?>("--hide-agent-reasoning");
        var disableStorageOpt = new Option<bool?>("--disable-response-storage");
        var noProjDocOpt = new Option<bool>("--no-project-doc", () => false);
        var lastMsgOpt = new Option<string?>("--output-last-message");
        var eventLogOpt = new Option<string?>("--event-log", "Path to save JSON event log");
        var envInheritOpt = new Option<ShellEnvironmentPolicyInherit?>("--env-inherit");
        var envIgnoreOpt = new Option<bool?>("--env-ignore-default-excludes");
        var envExcludeOpt = new Option<string[]>("--env-exclude") { AllowMultipleArgumentsPerToken = true };
        var envSetOpt = new Option<string[]>("--env-set") { AllowMultipleArgumentsPerToken = true };
        var envIncludeOpt = new Option<string[]>("--env-include-only") { AllowMultipleArgumentsPerToken = true };

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
        cmd.AddOption(effortOpt);
        cmd.AddOption(summaryOpt);
        cmd.AddOption(instrOpt);
        cmd.AddOption(hideReasonOpt);
        cmd.AddOption(disableStorageOpt);
        cmd.AddOption(noProjDocOpt);
        cmd.AddOption(lastMsgOpt);
        cmd.AddOption(eventLogOpt);
        cmd.AddOption(envInheritOpt);
        cmd.AddOption(envIgnoreOpt);
        cmd.AddOption(envExcludeOpt);
        cmd.AddOption(envSetOpt);
        cmd.AddOption(envIncludeOpt);

        var binder = new InteractiveBinder(promptArg, imagesOpt, modelOpt, profileOpt, providerOpt,
            fullAutoOpt, approvalOpt, sandboxOpt, colorOpt, skipGitOpt, cwdOpt, notifyOpt, overridesOpt,
            effortOpt, summaryOpt, instrOpt, hideReasonOpt, disableStorageOpt, lastMsgOpt, noProjDocOpt, eventLogOpt,
            envInheritOpt, envIgnoreOpt, envExcludeOpt, envSetOpt, envIncludeOpt);

        cmd.SetHandler(async (InteractiveOptions opts, string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath, opts.Profile);

            var ov = ConfigOverrides.Parse(opts.Overrides);
            if (ov.Overrides.Count > 0)
            {
                if (cfg == null) cfg = new AppConfig();
                ov.Apply(cfg);
                AnsiConsole.MarkupLine($"[yellow]{ov.Overrides.Count} override(s) applied[/]");
            }

            if (!opts.SkipGitRepoCheck && !GitUtils.IsInsideGitRepo(Environment.CurrentDirectory))
            {
                Console.Error.WriteLine("Not inside a git repo. Use --skip-git-repo-check to override.");
                return;
            }

            if (opts.Cwd != null) Environment.CurrentDirectory = opts.Cwd;

            var policy = cfg?.ShellEnvironmentPolicy ?? new ShellEnvironmentPolicy();
            if (opts.EnvInherit != null) policy.Inherit = opts.EnvInherit.Value;
            if (opts.EnvIgnoreDefaultExcludes != null) policy.IgnoreDefaultExcludes = opts.EnvIgnoreDefaultExcludes.Value;
            if (opts.EnvExclude.Length > 0) policy.Exclude = opts.EnvExclude.Select(EnvironmentVariablePattern.CaseInsensitive).ToList();
            if (opts.EnvSet.Length > 0)
                policy.Set = opts.EnvSet.Select(s => s.Split('=',2)).ToDictionary(p => p[0], p => p.Length>1?p[1]:string.Empty);
            if (opts.EnvIncludeOnly.Length > 0) policy.IncludeOnly = opts.EnvIncludeOnly.Select(EnvironmentVariablePattern.CaseInsensitive).ToList();
            var envMap = ExecEnv.Create(policy);

            if (opts.NotifyCommand.Length > 0)
                NotifyUtils.RunNotify(opts.NotifyCommand, "session_started", envMap);

            string? prompt = opts.Prompt;
            if (string.IsNullOrEmpty(prompt) || prompt == "-")
            {
                if (!Console.IsInputRedirected)
                {
                    var inst = opts.InstructionsPath != null && File.Exists(opts.InstructionsPath)
                        ? File.ReadAllText(opts.InstructionsPath)
                        : cfg != null ? ProjectDoc.GetUserInstructions(cfg, Environment.CurrentDirectory, opts.NoProjectDoc) : null;
                    if (!string.IsNullOrWhiteSpace(inst))
                        prompt = inst;
                    else
                    {
                        Console.Error.WriteLine("No prompt provided. Provide as argument or pipe via stdin.");
                        return;
                    }
                }
                else
                {
                    prompt = await Console.In.ReadToEndAsync();
                }
            }

            var opts2 = opts with { Prompt = prompt };
            RunInteractive(opts2, cfg);
            if (opts.NotifyCommand.Length > 0)
                NotifyUtils.RunNotify(opts.NotifyCommand, "session_complete", envMap);
            await Task.CompletedTask;
        }, binder, configOption, cdOption);
        return cmd;
    }

    private static void RunInteractive(InteractiveOptions opts, AppConfig? cfg)
    {
        var sessionId = SessionManager.CreateSession();
        var history = new List<string>();
        string? lastMessage = null;
        StreamWriter? logWriter = null;
        if (opts.EventLogFile != null)
            logWriter = new StreamWriter(opts.EventLogFile, append: false);
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
                AnsiConsole.MarkupLine("Available commands: /history, /reset, /quit, /help, /log, /config, /save <file>, /save-last <file>, /version, /sessions, /delete <id>");
                continue;
            }
            if (prompt.Equals("/log", StringComparison.OrdinalIgnoreCase))
            {
                var dir = cfg != null ? EnvUtils.GetLogDir(cfg) : Path.Combine(EnvUtils.FindCodexHome(), "log");
                AnsiConsole.MarkupLine($"Log dir: [blue]{dir}[/]");
                continue;
            }
            if (prompt.Equals("/version", StringComparison.OrdinalIgnoreCase))
            {
                var ver = typeof(Program).Assembly.GetName().Version?.ToString() ?? "?";
                AnsiConsole.MarkupLine($"Version: [blue]{ver}[/]");
                continue;
            }
            if (prompt.Equals("/sessions", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var info in SessionManager.ListSessionsWithInfo())
                    AnsiConsole.MarkupLine($"{info.Id} {info.Start:o}");
                continue;
            }
            if (prompt.StartsWith("/delete", StringComparison.OrdinalIgnoreCase))
            {
                var parts = prompt.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    AnsiConsole.MarkupLine("Usage: /delete <id>");
                    continue;
                }
                var id = parts[1];
                if (SessionManager.DeleteSession(id))
                    AnsiConsole.MarkupLine($"Deleted {id}");
                else
                    AnsiConsole.MarkupLine($"Session {id} not found");
                continue;
            }
            if (prompt.Equals("/config", StringComparison.OrdinalIgnoreCase))
            {
                if (cfg != null)
                {
                    AnsiConsole.MarkupLine($"Model: [blue]{cfg.Model}[/]");
                    if (!string.IsNullOrEmpty(cfg.ModelProvider))
                        AnsiConsole.MarkupLine($"Provider: [blue]{cfg.ModelProvider}[/]");
                    var codexHome = cfg.CodexHome ?? EnvUtils.FindCodexHome();
                    AnsiConsole.MarkupLine($"CodexHome: [blue]{codexHome}[/]");
                    AnsiConsole.MarkupLine($"Hide reasoning: [blue]{cfg.HideAgentReasoning}[/]");
                    AnsiConsole.MarkupLine($"Disable storage: [blue]{cfg.DisableResponseStorage}[/]");
                    if (cfg.ModelReasoningEffort != null)
                        AnsiConsole.MarkupLine($"Reasoning effort: [blue]{cfg.ModelReasoningEffort}[/]");
                    if (cfg.ModelReasoningSummary != null)
                        AnsiConsole.MarkupLine($"Reasoning summary: [blue]{cfg.ModelReasoningSummary}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("No config loaded");
                }
                continue;
            }
            if (prompt.StartsWith("/save-last", StringComparison.OrdinalIgnoreCase))
            {
                var parts = prompt.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2 || lastMessage == null)
                {
                    AnsiConsole.MarkupLine("Usage: /save-last <file>");
                    continue;
                }
                File.WriteAllText(parts[1], lastMessage);
                AnsiConsole.MarkupLine($"Saved last message to [green]{parts[1]}[/]");
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
            if (logWriter != null)
                logWriter.WriteLine(prompt);
            lastMessage = prompt;
            AnsiConsole.MarkupLine($"You typed: [blue]{prompt}[/]");
        }
        if (logWriter != null)
        {
            logWriter.Flush();
            logWriter.Dispose();
        }
        if (SessionManager.GetHistoryFile(sessionId) is { } path)
            AnsiConsole.MarkupLine($"History saved to [green]{path}[/]");
        if (opts.LastMessageFile != null && lastMessage != null)
            File.WriteAllText(opts.LastMessageFile, lastMessage);
    }
}
