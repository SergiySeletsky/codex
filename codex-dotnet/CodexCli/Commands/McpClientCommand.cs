using System.CommandLine;
using CodexCli.Util;
using System.Text.Json;

namespace CodexCli.Commands;

public static class McpClientCommand
{
    public static Command Create()
    {
        var cmd = new Command("mcp-client", "Run MCP client to list tools");
        var timeoutOpt = new Option<int>("--timeout", () => 10, "Initialization timeout in seconds");
        var jsonOpt = new Option<bool>("--json", description: "Output JSON");
        cmd.AddOption(timeoutOpt);
        cmd.AddOption(jsonOpt);
        var progArg = new Argument<string>("program");
        var argsArg = new Argument<string[]>("args") { Arity = ArgumentArity.ZeroOrMore };
        cmd.AddArgument(progArg);
        cmd.AddArgument(argsArg);
        cmd.SetHandler(async (string program, string[] args, int timeout, bool json) =>
        {
            using var client = await McpClient.StartAsync(program, args);
            var initParams = new InitializeRequestParams(
                new ClientCapabilities(null, null, null),
                new Implementation("codex-mcp-client", "1.0"),
                "2025-03-26");
            var resp = await client.SendRequestAsync("initialize", initParams, timeout);
            Console.Error.WriteLine($"initialize response: {resp.Result}");
            var toolsResp = await client.SendRequestAsync("tools/list", null, timeout);
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(toolsResp.Result, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                Console.WriteLine(toolsResp.Result?.ToString());
            }
        }, progArg, argsArg, timeoutOpt, jsonOpt);
        return cmd;
    }
}
