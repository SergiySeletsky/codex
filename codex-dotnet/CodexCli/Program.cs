using CodexCli.Config;

namespace CodexCli;

class Program
{
    static void Main(string[] args)
    {
        string? configPath = null;
        string? cd = null;
        var remaining = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--config":
                    if (i + 1 < args.Length) configPath = args[++i];
                    break;
                case "--cd":
                    if (i + 1 < args.Length) cd = args[++i];
                    break;
                default:
                    remaining.Add(args[i]);
                    break;
            }
        }

        if (cd != null) Environment.CurrentDirectory = cd;
        AppConfig? cfg = null;
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            cfg = AppConfig.Load(configPath);
        }

        var cmd = remaining.Count > 0 ? remaining[0] : "interactive";
        switch (cmd)
        {
            case "exec":
                var prompt = remaining.Count > 1 ? remaining[1] : string.Empty;
                Console.WriteLine($"Exec prompt: {prompt}");
                break;
            case "login":
                Console.WriteLine("Login flow not yet implemented.");
                break;
            case "mcp":
                Console.WriteLine("MCP server not yet implemented.");
                break;
            case "proto":
                Console.WriteLine("Protocol mode not yet implemented.");
                break;
            case "debug":
                if (remaining.Count > 1 && remaining[1] == "seatbelt")
                    Console.WriteLine("Seatbelt not implemented.");
                else if (remaining.Count > 1 && remaining[1] == "landlock")
                    Console.WriteLine("Landlock not implemented.");
                else
                    Console.WriteLine("Unknown debug command");
                break;
            case "interactive":
            default:
                Console.WriteLine("Interactive mode selected.");
                if (cfg?.NotifyCommand is { } notify)
                {
                    try { System.Diagnostics.Process.Start(notify, "session_started"); } catch { }
                }
                break;
        }
    }
}
