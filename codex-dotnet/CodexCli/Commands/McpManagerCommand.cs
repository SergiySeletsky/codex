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
        var rootsJsonOpt = new Option<bool>("--json", () => false);
        var rootsEventsOpt = new Option<string?>("--events-url");
        var rootsWatchOpt = new Option<bool>("--watch-events", () => false);

        var rootsList = new Command("list", "List roots");
        rootsList.AddOption(rootsServerOpt);
        rootsList.AddOption(rootsJsonOpt);
        rootsList.AddOption(rootsEventsOpt);
        rootsList.AddOption(rootsWatchOpt);
        rootsList.SetHandler(async (string? configPath, string server, bool json, string? eventsUrl, bool watch) =>
        {
            var cfg = configPath != null ? AppConfig.Load(configPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.ListRootsAsync(server);
            if (json)
                Console.WriteLine(JsonSerializer.Serialize(res.Roots));
            else
                foreach (var r in res.Roots) Console.WriteLine(r.Uri);
            if (watch && eventsUrl != null)
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(JsonSerializer.Serialize(ev));
        }, configOption, rootsServerOpt, rootsJsonOpt, rootsEventsOpt, rootsWatchOpt);

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
        var msgJsonOpt = new Option<bool>("--json", () => false);
        var msgEventsOpt = new Option<string?>("--events-url");
        var msgWatchOpt = new Option<bool>("--watch-events", () => false);
        msgList.AddOption(msgServerOpt);
        msgList.AddOption(msgJsonOpt);
        msgList.AddOption(msgEventsOpt);
        msgList.AddOption(msgWatchOpt);
        msgList.SetHandler(async (string? cfgPath, string server, bool json, string? eventsUrl, bool watch) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.ListMessagesAsync(server);
            if (json)
                Console.WriteLine(JsonSerializer.Serialize(res.Messages));
            else
                foreach (var m in res.Messages) Console.WriteLine(m);
            if (watch && eventsUrl != null)
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(JsonSerializer.Serialize(ev));
        }, configOption, msgServerOpt, msgJsonOpt, msgEventsOpt, msgWatchOpt);

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
        var msgAdd = new Command("add", "Add message");
        msgAdd.AddOption(msgServerOpt);
        var msgTextArg = new Argument<string>("text");
        msgAdd.AddArgument(msgTextArg);
        msgAdd.SetHandler(async (string? cfgPath, string server, string text) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            await mgr.AddMessageAsync(server, text);
            Console.WriteLine("ok");
        }, configOption, msgServerOpt, msgTextArg);

        var msgGet = new Command("get", "Get message entry");
        msgGet.AddOption(msgServerOpt);
        var msgOffsetArg = new Argument<int>("offset");
        msgGet.AddArgument(msgOffsetArg);
        msgGet.SetHandler(async (string? cfgPath, string server, int offset) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.GetMessageEntryAsync(server, offset);
            Console.WriteLine(res.Entry ?? "");
        }, configOption, msgServerOpt, msgOffsetArg);

        msgCmd.AddCommand(msgAdd);
        msgCmd.AddCommand(msgGet);
        root.AddCommand(msgCmd);

        // prompts subcommand derived from codex-rs mcp-cli (C# version done)
        var prmCmd = new Command("prompts", "Manage prompts on a server");
        var prmServerOpt = new Option<string>("--server", "Server name");
        var prmNameArg = new Argument<string>("name", () => string.Empty);
        var prmMsgArg = new Argument<string>("message", () => string.Empty);

        var prmList = new Command("list", "List prompts");
        var prmJsonOpt = new Option<bool>("--json", () => false);
        var prmEventsOpt = new Option<string?>("--events-url");
        var prmWatchOpt = new Option<bool>("--watch-events", () => false);
        prmList.AddOption(prmServerOpt);
        prmList.AddOption(prmJsonOpt);
        prmList.AddOption(prmEventsOpt);
        prmList.AddOption(prmWatchOpt);
        prmList.SetHandler(async (string? cfgPath, string server, bool json, string? eventsUrl, bool watch) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.ListPromptsAsync(server);
            if (json)
                Console.WriteLine(JsonSerializer.Serialize(res.Prompts));
            else
                foreach (var p in res.Prompts) Console.WriteLine(p.Name);
            if (watch && eventsUrl != null)
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(JsonSerializer.Serialize(ev));
        }, configOption, prmServerOpt, prmJsonOpt, prmEventsOpt, prmWatchOpt);

        var prmGet = new Command("get", "Get prompt");
        prmGet.AddOption(prmServerOpt);
        prmGet.AddOption(prmJsonOpt);
        prmGet.AddOption(prmEventsOpt);
        prmGet.AddOption(prmWatchOpt);
        prmGet.AddArgument(prmNameArg);
        prmGet.SetHandler(async (string? cfgPath, string server, bool json, string? eventsUrl, bool watch, string name) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.GetPromptAsync(server, name);
            if (json)
                Console.WriteLine(JsonSerializer.Serialize(res.Messages));
            else
                foreach (var m in res.Messages) Console.WriteLine(m.Content);
            if (watch && eventsUrl != null)
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(JsonSerializer.Serialize(ev));
        }, configOption, prmServerOpt, prmJsonOpt, prmEventsOpt, prmWatchOpt, prmNameArg);

        var prmAdd = new Command("add", "Add prompt");
        prmAdd.AddOption(prmServerOpt);
        prmAdd.AddArgument(prmNameArg);
        prmAdd.AddArgument(prmMsgArg);
        prmAdd.SetHandler(async (string? cfgPath, string server, string name, string message) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            await mgr.AddPromptAsync(server, name, message);
            Console.WriteLine("ok");
        }, configOption, prmServerOpt, prmNameArg, prmMsgArg);

        prmCmd.AddCommand(prmList);
        prmCmd.AddCommand(prmGet);
        prmCmd.AddCommand(prmAdd);
        root.AddCommand(prmCmd);

        // resources subcommand
        var resCmd = new Command("resources", "Manage server resources");
        var resServerOpt = new Option<string>("--server", "Server name");
        var resUriArg = new Argument<string>("uri", () => string.Empty);
        var textArg = new Argument<string>("text", () => string.Empty);

        var resList = new Command("list", "List resources");
        var resJsonOpt = new Option<bool>("--json", () => false);
        var resEventsOpt = new Option<string?>("--events-url");
        var resWatchOpt = new Option<bool>("--watch-events", () => false);
        resList.AddOption(resServerOpt);
        resList.AddOption(resJsonOpt);
        resList.AddOption(resEventsOpt);
        resList.AddOption(resWatchOpt);
        resList.SetHandler(async (string? cfgPath, string server, bool json, string? eventsUrl, bool watch) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.ListResourcesAsync(server);
            if (json)
                Console.WriteLine(JsonSerializer.Serialize(res.Resources));
            else
                foreach (var r in res.Resources) Console.WriteLine(r.Uri);
            if (watch && eventsUrl != null)
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(JsonSerializer.Serialize(ev));
        }, configOption, resServerOpt, resJsonOpt, resEventsOpt, resWatchOpt);

        var resRead = new Command("read", "Read resource");
        resRead.AddOption(resServerOpt);
        resRead.AddArgument(resUriArg);
        resRead.SetHandler(async (string? cfgPath, string server, string uri) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var elem = await mgr.ReadResourceAsync(server, uri);
            Console.WriteLine(elem.ToString());
        }, configOption, resServerOpt, resUriArg);

        var resWrite = new Command("write", "Write resource");
        resWrite.AddOption(resServerOpt);
        resWrite.AddArgument(resUriArg);
        resWrite.AddArgument(textArg);
        resWrite.SetHandler(async (string? cfgPath, string server, string uri, string text) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            await mgr.WriteResourceAsync(server, uri, text);
            Console.WriteLine("ok");
        }, configOption, resServerOpt, resUriArg, textArg);

        var resSub = new Command("subscribe", "Subscribe to resource updates");
        resSub.AddOption(resServerOpt);
        resSub.AddArgument(resUriArg);
        resSub.SetHandler(async (string? cfgPath, string server, string uri) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            await mgr.SubscribeAsync(server, uri);
            Console.WriteLine("ok");
        }, configOption, resServerOpt, resUriArg);

        var resUnsub = new Command("unsubscribe", "Unsubscribe from resource");
        resUnsub.AddOption(resServerOpt);
        resUnsub.AddArgument(resUriArg);
        resUnsub.SetHandler(async (string? cfgPath, string server, string uri) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            await mgr.UnsubscribeAsync(server, uri);
            Console.WriteLine("ok");
        }, configOption, resServerOpt, resUriArg);

        resCmd.AddCommand(resList);
        resCmd.AddCommand(resRead);
        resCmd.AddCommand(resWrite);
        resCmd.AddCommand(resSub);
        resCmd.AddCommand(resUnsub);
        root.AddCommand(resCmd);

        // templates subcommand
        var tmplCmd = new Command("templates", "List resource templates");
        var tmplServerOpt = new Option<string>("--server", "Server name");
        var tmplJsonOpt = new Option<bool>("--json", () => false);
        var tmplEventsOpt = new Option<string?>("--events-url");
        var tmplWatchOpt = new Option<bool>("--watch-events", () => false);
        tmplCmd.AddOption(tmplServerOpt);
        tmplCmd.AddOption(tmplJsonOpt);
        tmplCmd.AddOption(tmplEventsOpt);
        tmplCmd.AddOption(tmplWatchOpt);
        tmplCmd.SetHandler(async (string? cfgPath, string server, bool json, string? eventsUrl, bool watch) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.ListTemplatesAsync(server);
            if (json)
                Console.WriteLine(JsonSerializer.Serialize(res.ResourceTemplates));
            else
                foreach (var t in res.ResourceTemplates) Console.WriteLine(t.Uri);
            if (watch && eventsUrl != null)
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(JsonSerializer.Serialize(ev));
        }, configOption, tmplServerOpt, tmplJsonOpt, tmplEventsOpt, tmplWatchOpt);
        root.AddCommand(tmplCmd);

        // logging set-level
        var logCmd = new Command("set-level", "Set log level");
        var logServerOpt = new Option<string>("--server", "Server name");
        var logArg = new Argument<string>("level");
        logCmd.AddOption(logServerOpt);
        logCmd.AddArgument(logArg);
        logCmd.SetHandler(async (string? cfgPath, string server, string level) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            await mgr.SetLevelAsync(server, level);
            Console.WriteLine("ok");
        }, configOption, logServerOpt, logArg);
        root.AddCommand(logCmd);

        // complete subcommand
        var completeCmd = new Command("complete", "Request completion");
        var compServerOpt = new Option<string>("--server", "Server name");
        var prefixArg = new Argument<string>("prefix");
        completeCmd.AddOption(compServerOpt);
        completeCmd.AddArgument(prefixArg);
        completeCmd.SetHandler(async (string? cfgPath, string server, string prefix) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.CompleteAsync(server, prefix);
            Console.WriteLine(res.Completion.Values[0]);
        }, configOption, compServerOpt, prefixArg);
        root.AddCommand(completeCmd);

        // sampling create-message
        var sampleCmd = new Command("create-message", "Create sampling message");
        var smpServerOpt = new Option<string>("--server", "Server name");
        var msgContentArg = new Argument<string>("text");
        sampleCmd.AddOption(smpServerOpt);
        sampleCmd.AddArgument(msgContentArg);
        sampleCmd.SetHandler(async (string? cfgPath, string server, string text) =>
        {
            var cfg = cfgPath != null ? AppConfig.Load(cfgPath) : new AppConfig();
            var (mgr, _) = await McpConnectionManager.CreateAsync(cfg.McpServers);
            var res = await mgr.CreateMessageAsync(server, text);
            if (res.Content is CreateMessageTextContent txt)
                Console.WriteLine(txt.Text);
        }, configOption, smpServerOpt, msgContentArg);
        root.AddCommand(sampleCmd);
        return root;
    }
}
