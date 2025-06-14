using System.CommandLine;
using CodexCli.Config;

namespace CodexCli.Commands;

public static class McpCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var cmd = new Command("mcp", "Run as MCP server");
        cmd.SetHandler(async (string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);
            Console.WriteLine("MCP server not yet implemented.");
            await Task.CompletedTask;
        }, configOption, cdOption);
        return cmd;
    }
}
