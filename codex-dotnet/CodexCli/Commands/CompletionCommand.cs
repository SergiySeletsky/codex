using System.CommandLine;

namespace CodexCli.Commands;

public static class CompletionCommand
{
    public static Command Create(RootCommand root, Option<string?> configOption, Option<string?> cdOption)
    {
        var shellArg = new Argument<string>("shell", "bash|zsh|fish");
        var cmd = new Command("completion", "Generate shell completion script");
        cmd.AddArgument(shellArg);
        cmd.SetHandler(async (string shell, string? cfg, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            switch (shell.ToLowerInvariant())
            {
                case "bash":
                    await root.InvokeAsync("--help --shell=bash");
                    break;
                case "zsh":
                    await root.InvokeAsync("--help --shell=zsh");
                    break;
                case "fish":
                    await root.InvokeAsync("--help --shell=fish");
                    break;
                default:
                    Console.Error.WriteLine("Unknown shell: " + shell);
                    break;
            }
        }, shellArg, configOption, cdOption);
        return cmd;
    }
}
