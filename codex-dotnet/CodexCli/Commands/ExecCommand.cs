using System.CommandLine;
using CodexCli.Config;

namespace CodexCli.Commands;

public static class ExecCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var promptArg = new Argument<string>("prompt", () => string.Empty, "Prompt text");
        var cmd = new Command("exec", "Run Codex non-interactively");
        cmd.AddArgument(promptArg);
        cmd.SetHandler(async (string prompt, string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);
            Console.WriteLine($"Exec prompt: {prompt}");
            await Task.CompletedTask;
        }, promptArg, configOption, cdOption);
        return cmd;
    }
}
