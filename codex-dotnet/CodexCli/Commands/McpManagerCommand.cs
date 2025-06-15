using System.CommandLine;
using System.Text.Json;
using CodexCli.Config;
using CodexCli.Util;
using System;

namespace CodexCli.Commands;

public static class McpManagerCommand
{
    public static Command Create(Option<string?> configOption)
    {
        var root = new Command("mcp-manager", "Query multiple MCP servers from config");
        var eventsUrlOpt = new Option<string?>("--events-url");
        var watchOpt = new Option<bool>("--watch-events", description: "Stream events");

        var jsonOpt = new Option<bool>("--json", "Output JSON");

        var serverOpt = new Option<string?>("--server", "Only list tools from the specified server");

        var listCmd = new Command("list", "List tools from configured servers");
        listCmd.AddOption(eventsUrlOpt);
        listCmd.AddOption(watchOpt);
        listCmd.AddOption(jsonOpt);
        listCmd.AddOption(serverOpt);
        listCmd.SetHandler(async (string? configPath, string? eventsUrl, bool watch, bool json, string? server) =>
        {
            var cfg = configPath != null ? AppConfig.Load(configPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            await mgr.RefreshToolsAsync();
            Dictionary<string, Tool> tools;
            if (server != null)
            {
                tools = new();
                foreach (var t in await mgr.ListToolsAsync(server))
                    tools[McpConnectionManager.FullyQualifiedToolName(server, t.Name)] = t;
            }
            else
            {
                tools = mgr.ListAllTools();
            }

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
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(JsonSerializer.Serialize(ev));
            }
            if (watch && eventsUrl != null)
            {
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(JsonSerializer.Serialize(ev));
            }
        }, configOption, eventsUrlOpt, watchOpt, jsonOpt, serverOpt);

        var serversCmd = new Command("servers", "List configured server names");
        serversCmd.SetHandler((string? configPath) =>
        {
            var cfg = configPath != null ? AppConfig.Load(configPath) : new AppConfig();
            var (mgr, _) = McpConnectionManager.CreateAsync(cfg.McpServers).Result;
            foreach (var s in mgr.ListServers()) Console.WriteLine(s);
        }, configOption);

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
        root.AddCommand(serversCmd);
        var rootsCmd = new Command("roots", "Manage server roots");
        var rootsServerOpt = new Option<string>("--server", description: "Server name");
        var uriArg = new Argument<string>("uri", () => string.Empty);

        var rootsList = new Command("list", "List roots");
        rootsList.AddOption(rootsServerOpt);
        rootsList.SetHandler(async (string? configPath, string server) =>
        {
            var cfg = configPath != null ? AppConfig.Load(configPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.ListRootsAsync(server);
            foreach (var r in res.Roots) Console.WriteLine(r.Uri);
        }, configOption, rootsServerOpt);

        var rootsAdd = new Command("add", "Add root");
        rootsAdd.AddOption(rootsServerOpt);
        rootsAdd.AddArgument(uriArg);
        rootsAdd.SetHandler(async (string? configPath, string server, string uri) =>
        {
            var cfg = configPath != null ? AppConfig.Load(configPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            await mgr.AddRootAsync(server, uri);
            Console.WriteLine("ok");
        }, configOption, rootsServerOpt, uriArg);

        var rootsRemove = new Command("remove", "Remove root");
        rootsRemove.AddOption(rootsServerOpt);
        rootsRemove.AddArgument(uriArg);
        rootsRemove.SetHandler(async (string? configPath, string server, string uri) =>
        {
            var cfg = configPath != null ? AppConfig.Load(configPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            await mgr.RemoveRootAsync(server, uri);
            Console.WriteLine("ok");
        }, configOption, rootsServerOpt, uriArg);

        rootsCmd.AddCommand(rootsList);
        rootsCmd.AddCommand(rootsAdd);
        rootsCmd.AddCommand(rootsRemove);
        root.AddCommand(rootsCmd);

        // messages subcommand derived from codex-rs mcp-cli (C# version done)
        var msgCmd = new Command("messages", "Manage messages on a server");
        var msgServerOpt = new Option<string>("--server", "Server name");
        var termArg = new Argument<string>("term", () => string.Empty);
        var countArg = new Argument<int>("n", () => 10);

        var msgList = new Command("list", "List messages");
        msgList.AddOption(msgServerOpt);
        msgList.SetHandler(async (string? cfgPath, string server) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.ListMessagesAsync(server);
            foreach (var m in res.Messages) Console.WriteLine(m);
        }, configOption, msgServerOpt);

        var msgCount = new Command("count", "Count messages");
        msgCount.AddOption(msgServerOpt);
        msgCount.SetHandler(async (string? cfgPath, string server) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.CountMessagesAsync(server);
            Console.WriteLine(res.Count);
        }, configOption, msgServerOpt);

        var msgClear = new Command("clear", "Clear messages");
        msgClear.AddOption(msgServerOpt);
        msgClear.SetHandler(async (string? cfgPath, string server) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            await mgr.ClearMessagesAsync(server);
            Console.WriteLine("ok");
        }, configOption, msgServerOpt);

        var msgSearch = new Command("search", "Search messages");
        msgSearch.AddOption(msgServerOpt);
        msgSearch.AddArgument(termArg);
        msgSearch.SetHandler(async (string? cfgPath, string server, string term) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.SearchMessagesAsync(server, term);
            foreach (var m in res.Messages) Console.WriteLine(m);
        }, configOption, msgServerOpt, termArg);

        var msgLast = new Command("last", "Show last N messages");
        msgLast.AddOption(msgServerOpt);
        msgLast.AddArgument(countArg);
        msgLast.SetHandler(async (string? cfgPath, string server, int n) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.LastMessagesAsync(server, n);
            foreach (var m in res.Messages) Console.WriteLine(m);
        }, configOption, msgServerOpt, countArg);

        msgCmd.AddCommand(msgList);
        msgCmd.AddCommand(msgCount);
        msgCmd.AddCommand(msgClear);
        msgCmd.AddCommand(msgSearch);
        msgCmd.AddCommand(msgLast);
        root.AddCommand(msgCmd);
        return root;
    }
}
