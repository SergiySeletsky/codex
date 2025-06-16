using CodexCli;
using CodexCli.Config;
using CodexCli.Util;
using Spectre.Console;
using CodexCli.Interactive;

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

        // Basic integration of placeholder widgets. Real-time event streaming
        // like the Rust version is not yet implemented, but we display the
        // widgets so the UI structure matches. ChatWidget.cs and
        // StatusIndicatorWidget.cs mirror the Rust implementations.
        var chat = new ChatWidget();
        chat.AddAgentMessage("Codex ready.");
        using var status = new StatusIndicatorWidget();
        status.Start();

        var rc = await CodexCli.Program.Main(newArgs);

        status.Dispose();
        chat.Render();
        return rc;
    }
}
