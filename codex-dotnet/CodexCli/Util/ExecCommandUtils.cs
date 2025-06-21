using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CodexCli.Util;

/// <summary>
/// Helper methods for shell command formatting.
/// Mirrors codex-rs/tui/src/exec_command.rs (done).
/// </summary>
public static class ExecCommandUtils
{
    private static string EscapeArg(string arg)
    {
        if (Regex.IsMatch(arg, "^[a-zA-Z0-9_./+-]+$") )
            return arg;
        return "'" + arg.Replace("'", "'\\''") + "'";
    }

    public static string EscapeCommand(IEnumerable<string> command)
    {
        return string.Join(" ", command.Select(EscapeArg));
    }

    public static string StripBashLcAndEscape(IEnumerable<string> command)
    {
        var list = command.ToList();
        if (list.Count == 3 && list[0] == "bash" && list[1] == "-lc")
            return list[2];
        return EscapeCommand(list);
    }

    public static string? RelativizeToHome(string path)
    {
        var full = Path.GetFullPath(path);
        if (!Path.IsPathRooted(full))
            return null;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home))
            return null;
        if (full.StartsWith(home))
        {
            var rel = full.Substring(home.Length);
            rel = rel.TrimStart(Path.DirectorySeparatorChar);
            return rel.Length == 0 ? string.Empty : rel;
        }
        return null;
    }
}
