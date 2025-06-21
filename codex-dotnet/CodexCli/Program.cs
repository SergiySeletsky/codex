using CodexCli.Commands;
using System.CommandLine;
using CodexCli.Util;

// Rust CLI implemented in codex-rs/cli/src/main.rs (replay messages-only and follow parity tested)

namespace CodexCli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var configOption = new Option<string?>("--config", "Path to config file");
        var cdOption = new Option<string?>("--cd", "Change working directory");
        var logLevelOption = new Option<string?>("--log-level", "Logging level");

        var root = new RootCommand("Codex CLI");
        root.AddGlobalOption(configOption);
        root.AddGlobalOption(cdOption);
        root.AddGlobalOption(logLevelOption);

        root.AddCommand(ExecCommand.Create(configOption, cdOption));
        root.AddCommand(LoginCommand.Create(configOption, cdOption));
        root.AddCommand(McpCommand.Create(configOption, cdOption));
        root.AddCommand(ProtoCommand.Create(configOption, cdOption));
        root.AddCommand(DebugCommand.Create(configOption, cdOption));
        root.AddCommand(InteractiveCommand.Create(configOption, cdOption));
        root.AddCommand(CompletionCommand.Create(root, configOption, cdOption));
        root.AddCommand(HistoryCommand.Create());
        root.AddCommand(ReplayCommand.Create());
        root.AddCommand(ProviderCommand.Create(configOption));
        root.AddCommand(McpClientCommand.Create());
        root.AddCommand(McpManagerCommand.Create(configOption));
        root.AddCommand(ApplyPatchCommand.Create());
        var verCmd = new Command("version", "Print version");
        verCmd.SetHandler(() =>
        {
            Console.WriteLine(typeof(Program).Assembly.GetName().Version?.ToString() ?? "?");
        });
        root.AddCommand(verCmd);

        if (args.Length == 0)
        {
            args = new[] { "interactive" };
        }

        var logLevel = EnvUtils.GetLogLevel(root.Parse(args).GetValueForOption(logLevelOption));
        Console.WriteLine($"Log level: {logLevel}");
        return await root.InvokeAsync(args);
    }
}
