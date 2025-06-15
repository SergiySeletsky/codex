using System.CommandLine;
using System.CommandLine.Invocation;
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
        var pingOpt = new Option<bool>("--ping", description: "Send ping request and exit");
        var listRootsOpt = new Option<bool>("--list-roots", description: "List server roots and exit");
        var readResOpt = new Option<string?>("--read-resource", description: "URI to read from server");
        var writeResUriOpt = new Option<string?>("--write-resource-uri", description: "URI to write on server");
        var writeResTextOpt = new Option<string?>("--write-resource-text", description: "Text for resource");
        var listPromptsOpt = new Option<bool>("--list-prompts", description: "List prompts and exit");
        var getPromptOpt = new Option<string?>("--get-prompt", description: "Prompt name to fetch");
        var listTemplatesOpt = new Option<bool>("--list-templates", description: "List resource templates and exit");
        var setLevelOpt = new Option<string?>("--set-level", description: "Set server log level");
        var completeOpt = new Option<string?>("--complete", description: "Completion prefix");
        var subscribeOpt = new Option<string?>("--subscribe", description: "Subscribe to resource URI");
        var unsubscribeOpt = new Option<string?>("--unsubscribe", description: "Unsubscribe from resource URI");
        cmd.AddOption(timeoutOpt);
        cmd.AddOption(jsonOpt);
        cmd.AddOption(callOpt);
        cmd.AddOption(argsOpt);
        cmd.AddOption(envOpt);
        cmd.AddOption(pingOpt);
        cmd.AddOption(listRootsOpt);
        cmd.AddOption(readResOpt);
        cmd.AddOption(writeResUriOpt);
        cmd.AddOption(writeResTextOpt);
        cmd.AddOption(listPromptsOpt);
        cmd.AddOption(getPromptOpt);
        cmd.AddOption(listTemplatesOpt);
        cmd.AddOption(setLevelOpt);
        cmd.AddOption(completeOpt);
        cmd.AddOption(subscribeOpt);
        cmd.AddOption(unsubscribeOpt);
        var progArg = new Argument<string>("program");
        var argsArg = new Argument<string[]>("args") { Arity = ArgumentArity.ZeroOrMore };
        cmd.AddArgument(progArg);
        cmd.AddArgument(argsArg);
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var program = ctx.ParseResult.GetValueForArgument(progArg);
            var args = ctx.ParseResult.GetValueForArgument(argsArg);
            int timeout = ctx.ParseResult.GetValueForOption(timeoutOpt);
            bool json = ctx.ParseResult.GetValueForOption(jsonOpt);
            string? call = ctx.ParseResult.GetValueForOption(callOpt);
            string? arguments = ctx.ParseResult.GetValueForOption(argsOpt);
            string[] env = ctx.ParseResult.GetValueForOption(envOpt) ?? Array.Empty<string>();
            bool ping = ctx.ParseResult.GetValueForOption(pingOpt);
            bool listRoots = ctx.ParseResult.GetValueForOption(listRootsOpt);
            string? readResource = ctx.ParseResult.GetValueForOption(readResOpt);
            string? writeUri = ctx.ParseResult.GetValueForOption(writeResUriOpt);
            string? writeText = ctx.ParseResult.GetValueForOption(writeResTextOpt);
            bool listPrompts = ctx.ParseResult.GetValueForOption(listPromptsOpt);
            string? getPrompt = ctx.ParseResult.GetValueForOption(getPromptOpt);
            bool listTemplates = ctx.ParseResult.GetValueForOption(listTemplatesOpt);
            string? setLevel = ctx.ParseResult.GetValueForOption(setLevelOpt);
            string? completePrefix = ctx.ParseResult.GetValueForOption(completeOpt);
            string? subscribeUri = ctx.ParseResult.GetValueForOption(subscribeOpt);
            string? unsubscribeUri = ctx.ParseResult.GetValueForOption(unsubscribeOpt);

            var extraEnv = env.Select(e => e.Split('=', 2)).Where(p => p.Length == 2).ToDictionary(p => p[0], p => p[1]);
            using var client = await McpClient.StartAsync(program, args, extraEnv);
            var initParams = new InitializeRequestParams(
                new ClientCapabilities(null, null, null),
                new Implementation("codex-mcp-client", "1.0"),
                "2025-03-26");
            await client.InitializeAsync(initParams, timeout);

            if (ping)
            {
                await client.PingAsync(timeout);
                Console.WriteLine("pong");
            }
            else if (listRoots)
            {
                var roots = await client.ListRootsAsync(timeout);
                Console.WriteLine(JsonSerializer.Serialize(roots, new JsonSerializerOptions { WriteIndented = json }));
            }
            else if (listPrompts)
            {
                var res = await client.ListPromptsAsync(null, timeout);
                Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = json }));
            }
            else if (getPrompt != null)
            {
                var res = await client.GetPromptAsync(getPrompt, null, timeout);
                Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = json }));
            }
            else if (listTemplates)
            {
                var res = await client.ListResourceTemplatesAsync(null, timeout);
                Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = json }));
            }
            else if (readResource != null)
            {
                var res = await client.ReadResourceAsync(new ReadResourceRequestParams(readResource), timeout);
                Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = json }));
            }
            else if (writeUri != null)
            {
                await client.WriteResourceAsync(new WriteResourceRequestParams(writeUri, writeText ?? string.Empty), timeout);
                Console.WriteLine("ok");
            }
            else if (setLevel != null)
            {
                await client.SetLevelAsync(setLevel, timeout);
                Console.WriteLine("ok");
            }
            else if (subscribeUri != null)
            {
                await client.SubscribeAsync(new SubscribeRequestParams(subscribeUri), timeout);
                Console.WriteLine("ok");
            }
            else if (unsubscribeUri != null)
            {
                await client.UnsubscribeAsync(new UnsubscribeRequestParams(unsubscribeUri), timeout);
                Console.WriteLine("ok");
            }
            else if (completePrefix != null)
            {
                var p = new CompleteRequestParams(new CompleteRequestParamsArgument("text", completePrefix), new CompleteRequestParamsRef("mem:/"));
                var res = await client.CompleteAsync(p, timeout);
                Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = json }));
            }
            else if (call == null)
            {
                var tools = await client.ListToolsAsync(null, timeout);
                Console.WriteLine(JsonSerializer.Serialize(tools, new JsonSerializerOptions { WriteIndented = json }));
            }
            else
            {
                JsonElement? argsElem = null;
                if (!string.IsNullOrEmpty(arguments))
                    argsElem = JsonDocument.Parse(arguments).RootElement;
                var result = await client.CallToolAsync(call, argsElem, timeout);
                Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = json }));
            }
        });
        return cmd;
    }
}
