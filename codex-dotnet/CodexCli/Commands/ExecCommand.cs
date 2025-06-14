using System.CommandLine;
using CodexCli.Config;
using CodexCli.Util;
using CodexCli.Protocol;
using System.Linq;

namespace CodexCli.Commands;

public static class ExecCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var promptArg = new Argument<string?>("prompt", description: "Prompt text");
        var imagesOpt = new Option<FileInfo[]>("--image", "Image attachments") { AllowMultipleArgumentsPerToken = true };
        var modelOpt = new Option<string?>("--model", "Model to use");
        var profileOpt = new Option<string?>("--profile", "Config profile");
        var providerOpt = new Option<string?>("--model-provider", "Model provider");
        var fullAutoOpt = new Option<bool>("--full-auto", () => false, "Run in full-auto mode");
        var approvalOpt = new Option<ApprovalMode?>("--ask-for-approval", "When to require approval");
        var sandboxOpt = new Option<string[]>("-s", description: "Sandbox permissions") { AllowMultipleArgumentsPerToken = true };
        var colorOpt = new Option<ColorMode>("--color", () => ColorMode.Auto, "Output color mode");
        var cwdOpt = new Option<string?>(new[] {"--cwd", "-C"}, "Working directory for Codex");
        var lastMsgOpt = new Option<string?>("--output-last-message", "File to write last agent message");
        var skipGitOpt = new Option<bool>("--skip-git-repo-check", () => false, "Allow running outside git repo");
        var notifyOpt = new Option<string[]>("--notify", description: "Notification command") { AllowMultipleArgumentsPerToken = true };
        var overridesOpt = new Option<string[]>("-c", description: "Config overrides") { AllowMultipleArgumentsPerToken = true };

        var cmd = new Command("exec", "Run Codex non-interactively");
        cmd.AddArgument(promptArg);
        cmd.AddOption(imagesOpt);
        cmd.AddOption(modelOpt);
        cmd.AddOption(profileOpt);
        cmd.AddOption(providerOpt);
        cmd.AddOption(fullAutoOpt);
        cmd.AddOption(cwdOpt);
        cmd.AddOption(approvalOpt);
        cmd.AddOption(sandboxOpt);
        cmd.AddOption(colorOpt);
        cmd.AddOption(lastMsgOpt);
        cmd.AddOption(skipGitOpt);
        cmd.AddOption(notifyOpt);
        cmd.AddOption(overridesOpt);

        var binder = new ExecBinder(promptArg, imagesOpt, modelOpt, profileOpt, providerOpt, fullAutoOpt,
            approvalOpt, sandboxOpt, colorOpt, cwdOpt, lastMsgOpt, skipGitOpt, notifyOpt, overridesOpt);

        cmd.SetHandler(async (ExecOptions opts, string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);

            if (opts.NotifyCommand.Length > 0)
                NotifyUtils.RunNotify(opts.NotifyCommand, "session_started");

            if (!opts.SkipGitRepoCheck && !GitUtils.IsInsideGitRepo(Environment.CurrentDirectory))
            {
                Console.Error.WriteLine("Not inside a git repo. Use --skip-git-repo-check to override.");
                return;
            }

            if (opts.Cwd != null) Environment.CurrentDirectory = opts.Cwd;

            var prompt = opts.Prompt;
            if (string.IsNullOrEmpty(prompt) || prompt == "-")
            {
                Console.WriteLine("Reading prompt from stdin...");
                prompt = await Console.In.ReadToEndAsync();
            }

            var ov = ConfigOverrides.Parse(opts.Overrides);
            if (ov.Overrides.Count > 0)
            {
                Console.Error.WriteLine($"{ov.Overrides.Count} override(s) parsed");
            }

            var processor = new CodexCli.Protocol.EventProcessor(opts.Color != ColorMode.Never);
            processor.PrintConfigSummary(opts.Model ?? cfg?.Model ?? "default", Environment.CurrentDirectory, prompt.Trim());

            await foreach (var ev in CodexCli.Protocol.MockCodexAgent.RunAsync(prompt))
            {
                processor.ProcessEvent(ev);
                if (ev is TaskCompleteEvent tc && opts.LastMessageFile != null)
                {
                    await File.WriteAllTextAsync(opts.LastMessageFile, tc.LastAgentMessage ?? string.Empty);
                }
            }

            if (opts.NotifyCommand.Length > 0)
                NotifyUtils.RunNotify(opts.NotifyCommand, "session_complete");
        }, binder, configOption, cdOption);
        return cmd;
    }
}
