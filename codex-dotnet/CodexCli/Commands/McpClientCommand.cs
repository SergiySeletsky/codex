using System.CommandLine;
using CodexCli.Util;
using System.Text.Json;
using System.Linq;

namespace CodexCli.Commands;

public static class McpClientCommand
{
    public static Command Create()
    {
        var cmd = new Command("mcp-client", "Run MCP client to list or call tools");
        var timeoutOpt = new Option<int>("--timeout", () => 10, "Request timeout in seconds");
        var jsonOpt = new Option<bool>("--json", description: "Output JSON");
        var callOpt = new Option<string?>("--call", description: "Tool name to call");
        var argsOpt = new Option<string?>("--args", description: "JSON arguments for tool");
        var envOpt = new Option<string[]>("--env", description: "Extra VAR=VAL pairs", getDefaultValue: () => Array.Empty<string>());
        cmd.AddOption(timeoutOpt);
        cmd.AddOption(jsonOpt);
        cmd.AddOption(callOpt);
        cmd.AddOption(argsOpt);
        cmd.AddOption(envOpt);
        var progArg = new Argument<string>("program");
        var argsArg = new Argument<string[]>("args") { Arity = ArgumentArity.ZeroOrMore };
        cmd.AddArgument(progArg);
        cmd.AddArgument(argsArg);
        cmd.SetHandler(async (string program, string[] args, int timeout, bool json, string? call, string? arguments, string[] env) =>
        {
            var extraEnv = env.Select(e => e.Split('=', 2)).Where(p => p.Length == 2).ToDictionary(p => p[0], p => p[1]);
            using var client = await McpClient.StartAsync(program, args, extraEnv);
            var initParams = new InitializeRequestParams(
                new ClientCapabilities(null, null, null),
                new Implementation("codex-mcp-client", "1.0"),
                "2025-03-26");
            await client.InitializeAsync(initParams, timeout);

            if (call == null)
            {
                var tools = await client.ListToolsAsync(null, timeout);
                var obj = JsonSerializer.Serialize(tools, new JsonSerializerOptions { WriteIndented = json });
                Console.WriteLine(obj);
            }
            else
            {
                JsonElement? argsElem = null;
                if (!string.IsNullOrEmpty(arguments))
                    argsElem = JsonDocument.Parse(arguments).RootElement;
                var result = await client.CallToolAsync(call, argsElem, timeout);
                var obj = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = json });
                Console.WriteLine(obj);
            }
        }, progArg, argsArg, timeoutOpt, jsonOpt, callOpt, argsOpt, envOpt);
        return cmd;
    }
}
