using System.CommandLine;
using System.Text.Json;
using CodexCli.Config;
using CodexCli.Util;

namespace CodexCli.Commands;

public static class McpManagerCommand
{
    public static Command Create(Option<string?> configOption)
    {
        var root = new Command("mcp-manager", "Query multiple MCP servers from config");
        var eventsUrlOpt = new Option<string?>("--events-url");
        var watchOpt = new Option<bool>("--watch-events", description: "Stream events");

        var jsonOpt = new Option<bool>("--json", "Output JSON");

        var listCmd = new Command("list", "List all tools from configured servers");
        listCmd.AddOption(eventsUrlOpt);
        listCmd.AddOption(watchOpt);
        listCmd.AddOption(jsonOpt);
        listCmd.SetHandler(async (string? configPath, string? eventsUrl, bool watch, bool json) =>
        {
            var cfg = configPath != null ? AppConfig.Load(configPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            await mgr.RefreshToolsAsync();
            var tools = mgr.ListAllTools();
            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(tools.Keys));
            }
            else
            {
                foreach (var kv in tools)
                    Console.WriteLine($"{kv.Key}");
            }
            if (watch && eventsUrl != null)
            {
                await foreach (var line in McpEventStream.ReadLinesAsync(eventsUrl))
                    Console.WriteLine(line);
            }
        }, configOption, eventsUrlOpt, watchOpt, jsonOpt);

        var callCmd = new Command("call", "Call a tool using fully-qualified name");
        var nameArg = new Argument<string>("name");
        var argsOpt = new Option<string?>("--args", "JSON arguments");
        var timeoutOpt = new Option<int>("--timeout", () => 10);
        var callJsonOpt = new Option<bool>("--json", "Output JSON");
        callCmd.AddArgument(nameArg);
        callCmd.AddOption(argsOpt);
        callCmd.AddOption(timeoutOpt);
        callCmd.AddOption(callJsonOpt);
        callCmd.SetHandler(async (string? configPath, string name, string? argsJson, int timeout, bool json) =>
        {
            var cfg = configPath != null ? AppConfig.Load(configPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            JsonElement? args = null;
            if (argsJson != null)
                args = JsonDocument.Parse(argsJson).RootElement;
            var result = await mgr.CallToolAsync(name, args, TimeSpan.FromSeconds(timeout));
            var jsonStr = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            if (json) Console.WriteLine(jsonStr); else Console.WriteLine(jsonStr);
        }, configOption, nameArg, argsOpt, timeoutOpt, callJsonOpt);

        root.AddOption(configOption);
        root.AddCommand(listCmd);
        root.AddCommand(callCmd);
        return root;
    }
}
