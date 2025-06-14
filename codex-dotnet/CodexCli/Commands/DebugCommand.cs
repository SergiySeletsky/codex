using System.CommandLine;
using CodexCli.Config;

namespace CodexCli.Commands;

public static class DebugCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var cmd = new Command("debug", "Debug commands");

        var seatbeltArg = new Argument<string>("cmd");
        var seatbelt = new Command("seatbelt", "Run command under seatbelt");
        seatbelt.AddArgument(seatbeltArg);
        seatbelt.SetHandler(async (string c, string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);
            Console.WriteLine($"Seatbelt not implemented: {c}");
            await Task.CompletedTask;
        }, seatbeltArg, configOption, cdOption);

        var landlockArg = new Argument<string>("cmd");
        var landlock = new Command("landlock", "Run command under landlock");
        landlock.AddArgument(landlockArg);
        landlock.SetHandler(async (string c, string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);
            Console.WriteLine($"Landlock not implemented: {c}");
            await Task.CompletedTask;
        }, landlockArg, configOption, cdOption);

        cmd.AddCommand(seatbelt);
        cmd.AddCommand(landlock);
        return cmd;
    }
}
