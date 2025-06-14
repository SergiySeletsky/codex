using System.CommandLine;
using CodexCli.Config;
using CodexCli.Util;
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
        var sandboxOpt = new Option<SandboxPermission[]>("-s", description: "Sandbox permissions") { AllowMultipleArgumentsPerToken = true };
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

            Console.WriteLine($"Model: {opts.Model ?? cfg?.Model}");
            Console.WriteLine($"Provider: {opts.ModelProvider ?? "default"}");
            Console.WriteLine($"Profile: {opts.Profile}");
            Console.WriteLine($"Full auto: {opts.FullAuto}");
            Console.WriteLine($"Approval: {opts.Approval}");
            Console.WriteLine($"Sandbox: {string.Join(',', opts.Sandbox)}");
            Console.WriteLine($"Color: {opts.Color}");
            Console.WriteLine($"Cwd: {opts.Cwd}");
            Console.WriteLine($"Images: {string.Join(',', opts.Images.Select(i => i.FullName))}");
            Console.WriteLine($"Prompt: {prompt?.Trim()}");
            if (opts.NotifyCommand.Length > 0)
                Console.WriteLine($"Notify: {string.Join(' ', opts.NotifyCommand)}");
            if (ov.Overrides.Count > 0)
            {
                Console.WriteLine("Overrides:");
                foreach (var kv in ov.Overrides)
                    Console.WriteLine($"  {kv.Key}={kv.Value}");
            }
            if (opts.LastMessageFile != null)
                await File.WriteAllTextAsync(opts.LastMessageFile, "(last message placeholder)");
        }, binder, configOption, cdOption);
        return cmd;
    }
}
