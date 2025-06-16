using System.CommandLine;
using Spectre.Console;
using CodexCli.Config;
using CodexCli.Util;
using System.Linq;
using SessionManager = CodexCli.Util.SessionManager;
using CodexCli.Interactive;
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
        var docMaxOpt = new Option<int?>("--project-doc-max-bytes", "Limit size of AGENTS.md to read");
        var docPathOpt = new Option<string?>("--project-doc-path", "Explicit project doc path");

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
        cmd.AddOption(docMaxOpt);
        cmd.AddOption(docPathOpt);

        var binder = new InteractiveBinder(promptArg, imagesOpt, modelOpt, profileOpt, providerOpt,
            fullAutoOpt, approvalOpt, sandboxOpt, colorOpt, skipGitOpt, cwdOpt, notifyOpt, overridesOpt,
            effortOpt, summaryOpt, instrOpt, hideReasonOpt, disableStorageOpt, lastMsgOpt, noProjDocOpt, eventLogOpt,
            envInheritOpt, envIgnoreOpt, envExcludeOpt, envSetOpt, envIncludeOpt, docMaxOpt, docPathOpt);

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
                        : cfg != null ? ProjectDoc.GetUserInstructions(cfg, Environment.CurrentDirectory, opts.NoProjectDoc, opts.ProjectDocMaxBytes, opts.ProjectDocPath) : null;
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
            InteractiveApp.Run(opts2, cfg);
            if (opts.NotifyCommand.Length > 0)
                NotifyUtils.RunNotify(opts.NotifyCommand, "session_complete", envMap);
            await Task.CompletedTask;
        }, binder, configOption, cdOption);
        return cmd;
    }

}
