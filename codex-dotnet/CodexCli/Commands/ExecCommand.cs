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
        var effortOpt = new Option<ReasoningEffort?>("--reasoning-effort");
        var summaryOpt = new Option<ReasoningSummary?>("--reasoning-summary");
        var instrOpt = new Option<string?>("--instructions", "Path to instructions file");
        var hideReasonOpt = new Option<bool?>("--hide-agent-reasoning", "Hide reasoning events");
        var disableStorageOpt = new Option<bool?>("--disable-response-storage", "Disable response storage");

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
        cmd.AddOption(effortOpt);
        cmd.AddOption(summaryOpt);
        cmd.AddOption(instrOpt);
        cmd.AddOption(hideReasonOpt);
        cmd.AddOption(disableStorageOpt);

        var binder = new ExecBinder(promptArg, imagesOpt, modelOpt, profileOpt, providerOpt, fullAutoOpt,
            approvalOpt, sandboxOpt, colorOpt, cwdOpt, lastMsgOpt, skipGitOpt, notifyOpt, overridesOpt,
            effortOpt, summaryOpt, instrOpt, hideReasonOpt, disableStorageOpt);

        cmd.SetHandler(async (ExecOptions opts, string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath, opts.Profile);

            var sessionId = SessionManager.CreateSession();

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
                if (!Console.IsInputRedirected)
                {
                    var inst = opts.InstructionsPath != null && File.Exists(opts.InstructionsPath)
                        ? File.ReadAllText(opts.InstructionsPath)
                        : cfg != null ? ProjectDoc.GetUserInstructions(cfg, Environment.CurrentDirectory) : null;
                    if (!string.IsNullOrWhiteSpace(inst))
                    {
                        prompt = inst;
                    }
                    else
                    {
                        Console.WriteLine("Reading prompt from stdin...");
                        prompt = await Console.In.ReadToEndAsync();
                    }
                }
                else
                {
                    prompt = await Console.In.ReadToEndAsync();
                }
            }
            SessionManager.AddEntry(sessionId, prompt ?? string.Empty);

            var ov = ConfigOverrides.Parse(opts.Overrides);
            if (ov.Overrides.Count > 0)
            {
                Console.Error.WriteLine($"{ov.Overrides.Count} override(s) parsed");
            }

            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            var client = new OpenAIClient(apiKey);
            bool hideReason = opts.HideAgentReasoning ?? cfg?.HideAgentReasoning ?? false;
            bool disableStorage = opts.DisableResponseStorage ?? cfg?.DisableResponseStorage ?? false;
            var processor = new CodexCli.Protocol.EventProcessor(opts.Color != ColorMode.Never, !hideReason);
            processor.PrintConfigSummary(
                opts.Model ?? cfg?.Model ?? "default",
                opts.ModelProvider ?? cfg?.ModelProvider ?? string.Empty,
                Environment.CurrentDirectory,
                prompt.Trim(),
                disableStorage);

            await foreach (var ev in CodexCli.Protocol.MockCodexAgent.RunAsync(prompt))
            {
                processor.ProcessEvent(ev);
                switch (ev)
                {
                    case AgentMessageEvent am:
                        SessionManager.AddEntry(sessionId, am.Message);
                        break;
                    case ExecApprovalRequestEvent ar:
                        Console.Write($"Run '{string.Join(" ", ar.Command)}'? [y/N] ");
                        var resp = Console.ReadLine();
                        if (!resp?.StartsWith("y", StringComparison.OrdinalIgnoreCase) ?? true)
                            Console.WriteLine("Denied");
                        break;
                    case PatchApplyApprovalRequestEvent pr:
                        Console.Write($"Apply patch? [y/N] ");
                        var r = Console.ReadLine();
                        if (!r?.StartsWith("y", StringComparison.OrdinalIgnoreCase) ?? true)
                            Console.WriteLine("Patch denied");
                        break;
                    case TaskCompleteEvent tc:
                        var aiResp = await client.ChatAsync(prompt);
                        Console.WriteLine(aiResp);
                        if (opts.LastMessageFile != null)
                            await File.WriteAllTextAsync(opts.LastMessageFile, tc.LastAgentMessage ?? string.Empty);
                        break;
                }
            }

            if (SessionManager.GetHistoryFile(sessionId) is { } histPath)
                Console.WriteLine($"History saved to {histPath}");

            if (opts.NotifyCommand.Length > 0)
                NotifyUtils.RunNotify(opts.NotifyCommand, "session_complete");
        }, binder, configOption, cdOption);
        return cmd;
    }
}
