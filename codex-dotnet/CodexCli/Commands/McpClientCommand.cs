using System.CommandLine;
using System.CommandLine.Invocation;
using CodexCli.Util;
using System.Text.Json;
using System.Linq;
using System;

// C# port of `codex-rs/mcp-client/src/main.rs` (ping, list-roots and list-tools CLI parity tested)

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
        var addRootOpt = new Option<string?>("--add-root", description: "Add root directory");
        var removeRootOpt = new Option<string?>("--remove-root", description: "Remove root directory");
        var readResOpt = new Option<string?>("--read-resource", description: "URI to read from server");
        var writeResUriOpt = new Option<string?>("--write-resource-uri", description: "URI to write on server");
        var writeResTextOpt = new Option<string?>("--write-resource-text", description: "Text for resource");
        var listPromptsOpt = new Option<bool>("--list-prompts", description: "List prompts and exit");
        var getPromptOpt = new Option<string?>("--get-prompt", description: "Prompt name to fetch");
        var listTemplatesOpt = new Option<bool>("--list-templates", description: "List resource templates and exit");
        var setLevelOpt = new Option<string?>("--set-level", description: "Set server log level");
        var completeOpt = new Option<string?>("--complete", description: "Completion prefix");
        var createMsgOpt = new Option<string?>("--create-message", description: "Text for sampling/createMessage");
        var addPromptNameOpt = new Option<string?>("--add-prompt-name", description: "Name of prompt to add");
        var addPromptMsgOpt = new Option<string?>("--add-prompt-message", description: "System message for prompt");
        var addMessageOpt = new Option<string?>("--add-message", description: "Message text to store");
        var getMessageOpt = new Option<int?>("--get-message", description: "Fetch stored message by offset");
        var listMessagesOpt = new Option<bool>("--list-messages", description: "List all stored messages");
        var countMessagesOpt = new Option<bool>("--count-messages", description: "Print number of stored messages");
        var clearMessagesOpt = new Option<bool>("--clear-messages", description: "Delete all stored messages");
        var searchMessagesOpt = new Option<string?>("--search-messages", description: "Search messages for term");
        var lastMessagesOpt = new Option<int?>("--last-messages", description: "Show last N messages");
        var subscribeOpt = new Option<string?>("--subscribe", description: "Subscribe to resource URI");
        var unsubscribeOpt = new Option<string?>("--unsubscribe", description: "Unsubscribe from resource URI");
        var eventsUrlOpt = new Option<string?>("--events-url", description: "Stream events from server URL");
        var watchEventsOpt = new Option<bool>("--watch-events", description: "Watch server events");
        var codexOpt = new Option<bool>("--call-codex", description: "Call codex tool");
        var codexPromptOpt = new Option<string?>("--codex-prompt", description: "Prompt for codex tool");
        var codexModelOpt = new Option<string?>("--codex-model");
        var codexProviderOpt = new Option<string?>("--codex-provider");
        cmd.AddOption(timeoutOpt);
        cmd.AddOption(jsonOpt);
        cmd.AddOption(callOpt);
        cmd.AddOption(argsOpt);
        cmd.AddOption(envOpt);
        cmd.AddOption(pingOpt);
        cmd.AddOption(listRootsOpt);
        cmd.AddOption(addRootOpt);
        cmd.AddOption(removeRootOpt);
        cmd.AddOption(readResOpt);
        cmd.AddOption(writeResUriOpt);
        cmd.AddOption(writeResTextOpt);
        cmd.AddOption(listPromptsOpt);
        cmd.AddOption(getPromptOpt);
        cmd.AddOption(listTemplatesOpt);
        cmd.AddOption(setLevelOpt);
        cmd.AddOption(completeOpt);
        cmd.AddOption(createMsgOpt);
        cmd.AddOption(addPromptNameOpt);
        cmd.AddOption(addPromptMsgOpt);
        cmd.AddOption(addMessageOpt);
        cmd.AddOption(getMessageOpt);
        cmd.AddOption(listMessagesOpt);
        cmd.AddOption(countMessagesOpt);
        cmd.AddOption(clearMessagesOpt);
        cmd.AddOption(searchMessagesOpt);
        cmd.AddOption(lastMessagesOpt);
        cmd.AddOption(subscribeOpt);
        cmd.AddOption(unsubscribeOpt);
        cmd.AddOption(eventsUrlOpt);
        cmd.AddOption(watchEventsOpt);
        cmd.AddOption(codexOpt);
        cmd.AddOption(codexPromptOpt);
        cmd.AddOption(codexModelOpt);
        cmd.AddOption(codexProviderOpt);
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
            string? addRoot = ctx.ParseResult.GetValueForOption(addRootOpt);
            string? removeRoot = ctx.ParseResult.GetValueForOption(removeRootOpt);
            string? readResource = ctx.ParseResult.GetValueForOption(readResOpt);
            string? writeUri = ctx.ParseResult.GetValueForOption(writeResUriOpt);
            string? writeText = ctx.ParseResult.GetValueForOption(writeResTextOpt);
            bool listPrompts = ctx.ParseResult.GetValueForOption(listPromptsOpt);
            string? getPrompt = ctx.ParseResult.GetValueForOption(getPromptOpt);
            bool listTemplates = ctx.ParseResult.GetValueForOption(listTemplatesOpt);
            string? setLevel = ctx.ParseResult.GetValueForOption(setLevelOpt);
            string? completePrefix = ctx.ParseResult.GetValueForOption(completeOpt);
            string? addPromptName = ctx.ParseResult.GetValueForOption(addPromptNameOpt);
            string? addPromptMsg = ctx.ParseResult.GetValueForOption(addPromptMsgOpt);
            string? addMessage = ctx.ParseResult.GetValueForOption(addMessageOpt);
            int? getMessage = ctx.ParseResult.GetValueForOption(getMessageOpt);
            bool listMessages = ctx.ParseResult.GetValueForOption(listMessagesOpt);
            bool countMessages = ctx.ParseResult.GetValueForOption(countMessagesOpt);
            bool clearMessages = ctx.ParseResult.GetValueForOption(clearMessagesOpt);
            string? searchMessages = ctx.ParseResult.GetValueForOption(searchMessagesOpt);
            int? lastMessages = ctx.ParseResult.GetValueForOption(lastMessagesOpt);
            string? subscribeUri = ctx.ParseResult.GetValueForOption(subscribeOpt);
            string? unsubscribeUri = ctx.ParseResult.GetValueForOption(unsubscribeOpt);
            string? eventsUrl = ctx.ParseResult.GetValueForOption(eventsUrlOpt);
            bool watchEvents = ctx.ParseResult.GetValueForOption(watchEventsOpt);
            bool callCodex = ctx.ParseResult.GetValueForOption(codexOpt);
            string? codexPrompt = ctx.ParseResult.GetValueForOption(codexPromptOpt);
            string? codexModel = ctx.ParseResult.GetValueForOption(codexModelOpt);
            string? codexProvider = ctx.ParseResult.GetValueForOption(codexProviderOpt);
            string? createMessageText = ctx.ParseResult.GetValueForOption(createMsgOpt);

            var extraEnv = env.Select(e => e.Split('=', 2)).Where(p => p.Length == 2).ToDictionary(p => p[0], p => p[1]);
            using var client = await McpClient.StartAsync(program, args, extraEnv);
            var initParams = new InitializeRequestParams(
                new ClientCapabilities(null, null, null),
                new Implementation("codex-mcp-client", "1.0"),
                "2025-03-26");
            await client.InitializeAsync(initParams, timeout);

            if (watchEvents && eventsUrl != null)
            {
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(JsonSerializer.Serialize(ev));
                return;
            }

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
            else if (addRoot != null)
            {
                await client.AddRootAsync(addRoot, timeout);
                Console.WriteLine("ok");
            }
            else if (removeRoot != null)
            {
                await client.RemoveRootAsync(removeRoot, timeout);
                Console.WriteLine("ok");
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
            else if (addPromptName != null && addPromptMsg != null)
            {
                await client.AddPromptAsync(new AddPromptRequestParams(addPromptName, addPromptMsg), timeout);
                Console.WriteLine("ok");
            }
            else if (setLevel != null)
            {
                await client.SetLevelAsync(setLevel, timeout);
                Console.WriteLine("ok");
            }
            else if (addMessage != null)
            {
                await client.AddMessageAsync(addMessage, timeout);
                Console.WriteLine("ok");
            }
            else if (getMessage != null)
            {
                var res = await client.GetMessageEntryAsync(getMessage.Value, timeout);
                Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = json }));
            }
            else if (listMessages)
            {
                var res = await client.ListMessagesAsync(timeout);
                Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = json }));
            }
            else if (countMessages)
            {
                var res = await client.CountMessagesAsync(timeout);
                Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = json }));
            }
            else if (clearMessages)
            {
                await client.ClearMessagesAsync(timeout);
                Console.WriteLine("ok");
            }
            else if (searchMessages != null)
            {
                var res = await client.SearchMessagesAsync(searchMessages, 10, timeout);
                Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = json }));
            }
            else if (lastMessages != null)
            {
                var res = await client.LastMessagesAsync(lastMessages.Value, timeout);
                Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = json }));
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
            else if (callCodex)
            {
                var param = new CodexToolCallParam(codexPrompt ?? string.Empty, codexModel, null, null, null, null, null, codexProvider);
                var result = await client.CallCodexAsync(param, timeout);
                Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = json }));
            }
            else if (completePrefix != null)
            {
                var p = new CompleteRequestParams(new CompleteRequestParamsArgument("text", completePrefix), new CompleteRequestParamsRef("mem:/"));
                var res = await client.CompleteAsync(p, timeout);
                Console.WriteLine(JsonSerializer.Serialize(res, new JsonSerializerOptions { WriteIndented = json }));
            }
            else if (createMessageText != null)
            {
                var msg = new SamplingMessage(new SamplingTextContent(createMessageText), "user");
                var p = new CreateMessageRequestParams(new List<SamplingMessage> { msg }, 100);
                var res = await client.CreateMessageAsync(p, timeout);
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
