using System.CommandLine;
using CodexCli.Config;
using System.Net;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace CodexCli.Commands;

public static class McpCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var portOpt = new Option<int>("--port", () => 8080, "Port to listen on");
        var cmd = new Command("mcp", "Run as MCP server");
        cmd.AddOption(portOpt);
        cmd.SetHandler(async (string? cfgPath, string? cd, int port) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            using var server = new CodexCli.Util.McpServer(port);
            Console.WriteLine($"MCP server listening on port {port}");
            await server.RunAsync();
        }, configOption, cdOption, portOpt);
        return cmd;
    }
}
