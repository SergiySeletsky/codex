using System.CommandLine;
using CodexCli.Util;
using CodexCli.Config;

namespace CodexCli.Commands;

public static class HistoryCommand
{
    public static Command Create()
    {
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
        msgMetaCmd.SetHandler(async () =>
        {
            var cfg = new AppConfig();
            var meta = await MessageHistory.HistoryMetadataAsync(cfg);
            Console.WriteLine($"log {meta.LogId} count {meta.Count}");
        });

        var msgEntryCmd = new Command("messages-entry", "Show message history entry by offset");
        var msgOffsetArg = new Argument<int>("offset", "Entry offset");
        msgEntryCmd.AddArgument(msgOffsetArg);
        msgEntryCmd.SetHandler((int offset) =>
        {
            var cfg = new AppConfig();
            var text = MessageHistory.LookupEntry(0, offset, cfg);
            if (text != null) Console.WriteLine(text);
            else Console.WriteLine("not found");
        }, msgOffsetArg);

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
        msgSearchCmd.AddArgument(termArg);
        msgSearchCmd.SetHandler(async (string term) =>
        {
            var cfg = new AppConfig();
            var results = await MessageHistory.SearchEntriesAsync(term, cfg);
            foreach (var r in results) Console.WriteLine(r);
        }, termArg);

        var msgLastCmd = new Command("messages-last", "Show last N history entries");
        var lastCountArg = new Argument<int>("n", getDefaultValue: () => 10);
        var jsonOpt = new Option<bool>("--json", "Output JSON array");
        msgLastCmd.AddArgument(lastCountArg);
        msgLastCmd.AddOption(jsonOpt);
        msgLastCmd.SetHandler(async (int n, bool json) =>
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
        }, lastCountArg, jsonOpt);

        var msgCountCmd = new Command("messages-count", "Print number of history entries");
        msgCountCmd.SetHandler(async () =>
        {
            var cfg = new AppConfig();
            var count = await MessageHistory.CountEntriesAsync(cfg);
            Console.WriteLine(count);
        });

        var msgWatchCmd = new Command("messages-watch", "Watch for new history entries");
        msgWatchCmd.SetHandler(async () =>
        {
            var cfg = new AppConfig();
            await foreach (var line in MessageHistory.WatchEntriesAsync(cfg, CancellationToken.None))
                Console.WriteLine(line);
        });

        var statsCmd = new Command("stats", "Show message counts per session");
        statsCmd.SetHandler(async () =>
        {
            var cfg = new AppConfig();
            var stats = await MessageHistory.SessionStatsAsync(cfg);
            foreach (var kv in stats)
                Console.WriteLine($"{kv.Key} {kv.Value}");
        });

        var root = new Command("history", "Manage session history");
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
        return root;
    }
}
