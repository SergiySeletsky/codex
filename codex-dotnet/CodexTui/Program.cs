using CodexCli;
using CodexCli.Config;
using CodexCli.Util;
using Spectre.Console;
using CodexCli.Interactive;
using CodexCli.Protocol;

class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Parse common arguments we care about before delegating to CodexCli.
        string? configPath = null;
        string? providerId = null;
        bool skipGit = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length)
                configPath = args[i + 1];
            if (args[i] == "--model-provider" && i + 1 < args.Length)
                providerId = args[i + 1];
            if (args[i] == "--skip-git-repo-check")
                skipGit = true;
        }

        AppConfig? cfg = null;
        if (configPath != null && File.Exists(configPath))
            cfg = AppConfig.Load(configPath);
        providerId ??= cfg?.ModelProvider ?? "openai";

        if (!skipGit && !GitUtils.IsInsideGitRepo(Environment.CurrentDirectory))
        {
            if (!GitWarningScreen.ShowAndAsk())
                return 1;
        }

        if (ModelProviderInfo.BuiltIns.TryGetValue(providerId, out var info))
        {
            if (ApiKeyManager.GetKey(info) == null && info.EnvKey != null)
            {
                LoginScreen.Show();
                return 1;
            }
        }

        var newArgs = new string[args.Length + 1];
        newArgs[0] = "interactive";
        Array.Copy(args, 0, newArgs, 1, args.Length);

        // Provide interactive approval handler using the UserApprovalWidget.
        var widget = new UserApprovalWidget();
        InteractiveApp.ApprovalHandler = ev =>
        {
            return Task.FromResult(ev switch
            {
                ExecApprovalRequestEvent e => widget.PromptExec(e.Command.ToArray(), e.Cwd),
                PatchApplyApprovalRequestEvent p => widget.PromptPatch(p.PatchSummary),
                _ => ReviewDecision.Denied
            });
        };

        try
        {
            return await CodexCli.Program.Main(newArgs);
        }
        finally
        {
            InteractiveApp.ApprovalHandler = null;
        }
    }
}
