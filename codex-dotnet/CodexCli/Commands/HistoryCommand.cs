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

        var root = new Command("history", "Manage session history");
        root.AddCommand(listCmd);
        root.AddCommand(showCmd);
        root.AddCommand(clearCmd);
        return root;
    }
}
