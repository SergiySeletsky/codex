using System.CommandLine;
using CodexCli.Config;
using CodexCli.Util;

namespace CodexCli.Commands;

public static class DebugCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var cmd = new Command("debug", "Debug commands");

        var seatbeltArg = new Argument<string>("cmd");
        var seatbelt = new Command("seatbelt", "Run command under seatbelt");
        seatbelt.AddArgument(seatbeltArg);
        seatbelt.SetHandler((string c, string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            RunProcess(c, cfgPath);
        }, seatbeltArg, configOption, cdOption);

        var landlockArg = new Argument<string>("cmd");
        var landlock = new Command("landlock", "Run command under landlock");
        landlock.AddArgument(landlockArg);
        landlock.SetHandler((string c, string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            RunProcess(c, cfgPath);
        }, landlockArg, configOption, cdOption);

        cmd.AddCommand(seatbelt);
        cmd.AddCommand(landlock);
        return cmd;
    }

    private static void RunProcess(string command, string? configPath)
    {
        var parts = command.Split(' ', 2);
        var psi = new System.Diagnostics.ProcessStartInfo(parts[0])
        {
            UseShellExecute = false
        };
        if (parts.Length > 1)
            psi.ArgumentList.Add(parts[1]);
        if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
        {
            var cfg = AppConfig.Load(configPath);
            var env = ExecEnv.Create(cfg.ShellEnvironmentPolicy);
            foreach (var (k,v) in env)
                psi.Environment[k] = v;
        }
        var proc = System.Diagnostics.Process.Start(psi)!;
        ExitStatus.ExitWith(proc);
    }
}
