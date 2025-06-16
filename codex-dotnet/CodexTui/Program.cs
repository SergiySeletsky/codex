using CodexCli;
using CodexCli.Config;
using CodexCli.Util;
using Spectre.Console;

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
            AnsiConsole.MarkupLine("We recommend running codex inside a git repository. This helps ensure that changes can be tracked and easily rolled back if necessary.");
            AnsiConsole.Markup("Do you wish to proceed? [yellow](y/n)[/] ");
            var key = Console.ReadKey(true);
            if (key.KeyChar != 'y' && key.KeyChar != 'Y')
                return 1;
        }

        if (ModelProviderInfo.BuiltIns.TryGetValue(providerId, out var info))
        {
            if (ApiKeyManager.GetKey(info) == null && info.EnvKey != null)
            {
                Console.WriteLine("Login using `codex login` and then run this command again. 'q' to quit.");
                while (Console.ReadKey(true).KeyChar != 'q') { }
                return 1;
            }
        }

        var newArgs = new string[args.Length + 1];
        newArgs[0] = "interactive";
        Array.Copy(args, 0, newArgs, 1, args.Length);
        return await CodexCli.Program.Main(newArgs);
    }
}
