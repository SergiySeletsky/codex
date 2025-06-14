using CodexCli.Commands;
using System.CommandLine;

namespace CodexCli;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var configOption = new Option<string?>("--config", "Path to config file");
        var cdOption = new Option<string?>("--cd", "Change working directory");

        var root = new RootCommand("Codex CLI");
        root.AddGlobalOption(configOption);
        root.AddGlobalOption(cdOption);

        root.AddCommand(ExecCommand.Create(configOption, cdOption));
        root.AddCommand(LoginCommand.Create(configOption, cdOption));
        root.AddCommand(McpCommand.Create(configOption, cdOption));
        root.AddCommand(ProtoCommand.Create(configOption, cdOption));
        root.AddCommand(DebugCommand.Create(configOption, cdOption));
        root.AddCommand(InteractiveCommand.Create(configOption, cdOption));

        if (args.Length == 0)
        {
            args = new[] { "interactive" };
        }

        return await root.InvokeAsync(args);
    }
}
