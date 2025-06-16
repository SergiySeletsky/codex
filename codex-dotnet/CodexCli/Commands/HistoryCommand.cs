using System.CommandLine;
using CodexCli.Util;
using CodexCli.Config;
using CodexCli.Protocol;

namespace CodexCli.Commands;

public static class HistoryCommand
{
    public static Command Create()
    {
        var eventsUrlOpt = new Option<string?>("--events-url");
        var watchOpt = new Option<bool>("--watch-events", () => false);
        var listCmd = new Command("list", "List saved session IDs");
        listCmd.SetHandler(() =>
        {
            foreach (var id in SessionManager.ListSessions())
                Console.WriteLine(id);
        });

        var showCmd = new Command("show", "Print history for a session");
        var idArg = new Argument<string>("id", "Session ID");
        showCmd.AddArgument(idArg);
        showCmd.SetHandler((string id) =>
        {
            foreach (var line in SessionManager.GetHistory(id))
                Console.WriteLine(line);
        }, idArg);

        var clearCmd = new Command("clear", "Delete a saved session");
        clearCmd.AddArgument(idArg);
        clearCmd.SetHandler((string id) =>
        {
            if (SessionManager.DeleteSession(id))
                Console.WriteLine($"Deleted {id}");
            else
                Console.WriteLine($"Session {id} not found");
        }, idArg);

        var pathCmd = new Command("path", "Show history file path");
        pathCmd.AddArgument(idArg);
        pathCmd.SetHandler((string id) =>
        {
            var p = SessionManager.GetHistoryFile(id);
            if (p != null)
                Console.WriteLine(p);
        }, idArg);

        var purgeCmd = new Command("purge", "Delete all session history");
        purgeCmd.SetHandler(() =>
        {
            SessionManager.DeleteAllSessions();
            Console.WriteLine("All sessions deleted");
        });

        var infoCmd = new Command("info", "List sessions with start times");
        infoCmd.SetHandler(() =>
        {
            foreach (var info in SessionManager.ListSessionsWithInfo())
                Console.WriteLine($"{info.Id} {info.Start:o}");
        });

        var entryCmd = new Command("entry", "Show a single history entry");
        var offsetArg = new Argument<int>("offset", "Entry offset");
        entryCmd.AddArgument(idArg);
        entryCmd.AddArgument(offsetArg);
        entryCmd.SetHandler((string id, int offset) =>
        {
            var line = SessionManager.GetHistoryEntry(id, offset);
            if (line != null) Console.WriteLine(line);
            else Console.WriteLine("not found");
        }, idArg, offsetArg);

        var msgMetaCmd = new Command("messages-meta", "Show message history metadata");
        msgMetaCmd.AddOption(eventsUrlOpt);
        msgMetaCmd.AddOption(watchOpt);
        msgMetaCmd.SetHandler(async (string? eventsUrl, bool watch) =>
        {
            var cfg = new AppConfig();
            var meta = await MessageHistory.HistoryMetadataAsync(cfg);
            Console.WriteLine($"log {meta.LogId} count {meta.Count}");
            if (watch && eventsUrl != null)
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(ev));
        }, eventsUrlOpt, watchOpt);

        var msgEntryCmd = new Command("messages-entry", "Show message history entry by offset");
        var msgOffsetArg = new Argument<int>("offset", "Entry offset");
        msgEntryCmd.AddArgument(msgOffsetArg);
        msgEntryCmd.AddOption(eventsUrlOpt);
        msgEntryCmd.AddOption(watchOpt);
        msgEntryCmd.SetHandler(async (int offset, string? eventsUrl, bool watch) =>
        {
            var cfg = new AppConfig();
            var text = MessageHistory.LookupEntry(0, offset, cfg);
            if (text != null) Console.WriteLine(text);
            else Console.WriteLine("not found");
            if (watch && eventsUrl != null)
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(ev));
        }, msgOffsetArg, eventsUrlOpt, watchOpt);

        var msgPathCmd = new Command("messages-path", "Print message history file path");
        msgPathCmd.SetHandler(() =>
        {
            var cfg = new AppConfig();
            Console.WriteLine(MessageHistory.GetHistoryFile(cfg));
        });

        var msgClearCmd = new Command("messages-clear", "Delete message history file");
        msgClearCmd.SetHandler(() =>
        {
            var cfg = new AppConfig();
            MessageHistory.ClearHistory(cfg);
        });

        var msgSearchCmd = new Command("messages-search", "Search message history for text");
        var termArg = new Argument<string>("term", "Search term");
        var searchJsonOpt = new Option<bool>("--json", () => false, "Output JSON array");
        msgSearchCmd.AddArgument(termArg);
        msgSearchCmd.AddOption(searchJsonOpt);
        msgSearchCmd.AddOption(eventsUrlOpt);
        msgSearchCmd.AddOption(watchOpt);
        msgSearchCmd.SetHandler(async (string term, bool json, string? eventsUrl, bool watch) =>
        {
            var cfg = new AppConfig();
            var results = await MessageHistory.SearchEntriesAsync(term, cfg);
            if (json)
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(results));
            else
                foreach (var r in results) Console.WriteLine(r);
            if (watch && eventsUrl != null)
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(ev));
        }, termArg, searchJsonOpt, eventsUrlOpt, watchOpt);

        var msgLastCmd = new Command("messages-last", "Show last N history entries");
        var lastCountArg = new Argument<int>("n", getDefaultValue: () => 10);
        var jsonOpt = new Option<bool>("--json", "Output JSON array");
        msgLastCmd.AddArgument(lastCountArg);
        msgLastCmd.AddOption(jsonOpt);
        msgLastCmd.AddOption(eventsUrlOpt);
        msgLastCmd.AddOption(watchOpt);
        msgLastCmd.SetHandler(async (int n, bool json, string? eventsUrl, bool watch) =>
        {
            var cfg = new AppConfig();
            var lines = await MessageHistory.LastEntriesAsync(n, cfg);
            if (json)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(lines));
            }
            else
            {
                foreach (var l in lines) Console.WriteLine(l);
            }
            if (watch && eventsUrl != null)
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(ev));
        }, lastCountArg, jsonOpt, eventsUrlOpt, watchOpt);

        var msgCountCmd = new Command("messages-count", "Print number of history entries");
        var countJsonOpt = new Option<bool>("--json", () => false, "Output JSON object");
        msgCountCmd.AddOption(countJsonOpt);
        msgCountCmd.AddOption(eventsUrlOpt);
        msgCountCmd.AddOption(watchOpt);
        msgCountCmd.SetHandler(async (bool json, string? eventsUrl, bool watch) =>
        {
            var cfg = new AppConfig();
            var count = await MessageHistory.CountEntriesAsync(cfg);
            if (json)
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { count }));
            else
                Console.WriteLine(count);
            if (watch && eventsUrl != null)
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(ev));
        }, countJsonOpt, eventsUrlOpt, watchOpt);

        var msgWatchCmd = new Command("messages-watch", "Watch for new history entries");
        msgWatchCmd.SetHandler(async () =>
        {
            var cfg = new AppConfig();
            await foreach (var line in MessageHistory.WatchEntriesAsync(cfg, CancellationToken.None))
                Console.WriteLine(line);
        });

        var statsCmd = new Command("stats", "Show message counts per session");
        var statsJsonOpt = new Option<bool>("--json", () => false, "Output JSON map");
        statsCmd.AddOption(statsJsonOpt);
        statsCmd.AddOption(eventsUrlOpt);
        statsCmd.AddOption(watchOpt);
        statsCmd.SetHandler(async (bool json, string? eventsUrl, bool watch) =>
        {
            var cfg = new AppConfig();
            var stats = await MessageHistory.SessionStatsAsync(cfg);
            if (json)
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(stats));
            else
                foreach (var kv in stats)
                    Console.WriteLine($"{kv.Key} {kv.Value}");
            if (watch && eventsUrl != null)
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(ev));
        }, statsJsonOpt, eventsUrlOpt, watchOpt);

        var summaryCmd = new Command("summary", "List sessions with start time and message count");
        var summaryJsonOpt = new Option<bool>("--json", () => false, "Output JSON list");
        summaryCmd.AddOption(summaryJsonOpt);
        summaryCmd.AddOption(eventsUrlOpt);
        summaryCmd.AddOption(watchOpt);
        summaryCmd.SetHandler(async (bool json, string? eventsUrl, bool watch) =>
        {
            var cfg = new AppConfig();
            var stats = await MessageHistory.SessionStatsAsync(cfg);
            if (json)
            {
                var list = SessionManager.ListSessionsWithInfo()
                    .Select(i => new { id = i.Id, start = i.Start, count = stats.TryGetValue(i.Id, out var c) ? c : 0 })
                    .ToList();
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(list));
            }
            else
            {
                foreach (var info in SessionManager.ListSessionsWithInfo())
                {
                    stats.TryGetValue(info.Id, out var c);
                    Console.WriteLine($"{info.Id} {info.Start:o} {c}");
                }
            }
            if (watch && eventsUrl != null)
                await foreach (var ev in McpEventStream.ReadEventsAsync(eventsUrl))
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(ev));
        }, summaryJsonOpt, eventsUrlOpt, watchOpt);

        var root = new Command("history", "Manage session history");
        root.AddOption(eventsUrlOpt);
        root.AddOption(watchOpt);
        root.AddCommand(listCmd);
        root.AddCommand(showCmd);
        root.AddCommand(clearCmd);
        root.AddCommand(pathCmd);
        root.AddCommand(purgeCmd);
        root.AddCommand(entryCmd);
        root.AddCommand(infoCmd);
        root.AddCommand(msgMetaCmd);
        root.AddCommand(msgEntryCmd);
        root.AddCommand(msgPathCmd);
        root.AddCommand(msgClearCmd);
        root.AddCommand(msgSearchCmd);
        root.AddCommand(msgLastCmd);
        root.AddCommand(msgCountCmd);
        root.AddCommand(msgWatchCmd);
        root.AddCommand(statsCmd);
        root.AddCommand(summaryCmd);
        return root;
    }
}
