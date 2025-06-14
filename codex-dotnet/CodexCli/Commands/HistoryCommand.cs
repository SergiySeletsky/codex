using System.CommandLine;
using CodexCli.Util;

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

        var root = new Command("history", "Manage session history");
        root.AddCommand(listCmd);
        root.AddCommand(showCmd);
        root.AddCommand(clearCmd);
        root.AddCommand(pathCmd);
        root.AddCommand(purgeCmd);
        root.AddCommand(entryCmd);
        root.AddCommand(infoCmd);
        return root;
    }
}
