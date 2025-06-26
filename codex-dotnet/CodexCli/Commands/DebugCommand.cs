using System.CommandLine;
using CodexCli.Config;
using CodexCli.Util;

// Ported from codex-rs/cli/src/debug_sandbox.rs
// Cross-CLI parity tested in CrossCliCompatTests.DebugSeatbeltMatches, DebugLandlockMatches and DebugHelpMatches

namespace CodexCli.Commands;

public static class DebugCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var cmd = new Command("debug", "Debug commands");
        var envInheritOpt = new Option<ShellEnvironmentPolicyInherit?>("--env-inherit");
        var envIgnoreOpt = new Option<bool?>("--env-ignore-default-excludes");
        var envExcludeOpt = new Option<string[]>("--env-exclude") { AllowMultipleArgumentsPerToken = true };
        var envSetOpt = new Option<string[]>("--env-set") { AllowMultipleArgumentsPerToken = true };
        var envIncludeOpt = new Option<string[]>("--env-include-only") { AllowMultipleArgumentsPerToken = true };

        var seatbeltArg = new Argument<string>("cmd");
        var seatbelt = new Command("seatbelt", "Run command under seatbelt");
        seatbelt.AddArgument(seatbeltArg);
        seatbelt.AddOption(envInheritOpt);
        seatbelt.AddOption(envIgnoreOpt);
        seatbelt.AddOption(envExcludeOpt);
        seatbelt.AddOption(envSetOpt);
        seatbelt.AddOption(envIncludeOpt);
        seatbelt.SetHandler((string c, string? cfgPath, string? cd, ShellEnvironmentPolicyInherit? inh, bool? ign, string[] exc, string[] set, string[] inc) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            RunProcess(c, cfgPath, inh, ign, exc, set, inc);
        }, seatbeltArg, configOption, cdOption, envInheritOpt, envIgnoreOpt, envExcludeOpt, envSetOpt, envIncludeOpt);

        var landlockArg = new Argument<string>("cmd");
        var landlock = new Command("landlock", "Run command under landlock");
        landlock.AddArgument(landlockArg);
        landlock.AddOption(envInheritOpt);
        landlock.AddOption(envIgnoreOpt);
        landlock.AddOption(envExcludeOpt);
        landlock.AddOption(envSetOpt);
        landlock.AddOption(envIncludeOpt);
        landlock.SetHandler((string c, string? cfgPath, string? cd, ShellEnvironmentPolicyInherit? inh, bool? ign, string[] exc, string[] set, string[] inc) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            RunProcess(c, cfgPath, inh, ign, exc, set, inc);
        }, landlockArg, configOption, cdOption, envInheritOpt, envIgnoreOpt, envExcludeOpt, envSetOpt, envIncludeOpt);

        cmd.AddCommand(seatbelt);
        cmd.AddCommand(landlock);
        return cmd;
    }

    private static void RunProcess(string command, string? configPath,
        ShellEnvironmentPolicyInherit? inh, bool? ign, string[] exc, string[] set, string[] inc)
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
            var policy = cfg.ShellEnvironmentPolicy;
            if (inh != null) policy.Inherit = inh.Value;
            if (ign != null) policy.IgnoreDefaultExcludes = ign.Value;
            if (exc.Length > 0) policy.Exclude = exc.Select(EnvironmentVariablePattern.CaseInsensitive).ToList();
            if (set.Length > 0) policy.Set = set.Select(s => s.Split('=',2)).ToDictionary(p=>p[0], p=>p.Length>1?p[1]:string.Empty);
            if (inc.Length > 0) policy.IncludeOnly = inc.Select(EnvironmentVariablePattern.CaseInsensitive).ToList();
            var env = ExecEnv.Create(policy);
            foreach (var (k,v) in env)
                psi.Environment[k] = v;
        }
        var proc = System.Diagnostics.Process.Start(psi)!;
        ExitStatus.ExitWith(proc);
    }
}
