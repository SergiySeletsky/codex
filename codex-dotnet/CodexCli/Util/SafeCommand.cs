using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CodexCli.Util;

/// <summary>
/// Helpers for determining whether shell commands are known safe.
/// Mirrors codex-rs/core/src/is_safe_command.rs (done).
/// </summary>
public static class SafeCommand
{
    private static readonly HashSet<string> SimpleSafe = new(
        new[]{"cat","cd","echo","grep","head","ls","pwd","rg","tail","wc","which"});

    private static readonly string[] UnsafeFindOptions = new[]{
        "-exec","-execdir","-ok","-okdir","-delete",
        "-fls","-fprint","-fprint0","-fprintf"
    };

    private static bool IsValidSedNArg(string? arg)
    {
        if (string.IsNullOrEmpty(arg)) return false;
        if (!arg!.EndsWith("p")) return false;
        var core = arg.Substring(0, arg.Length - 1);
        var parts = core.Split(',');
        if (parts.Length == 1) return parts[0].All(char.IsDigit) && parts[0].Length > 0;
        if (parts.Length == 2) return parts.All(p => p.Length > 0 && p.All(char.IsDigit));
        return false;
    }

    public static bool IsSafeToCallWithExec(IReadOnlyList<string> command)
    {
        if (command.Count == 0) return false;
        var cmd0 = command[0];
        switch (cmd0)
        {
            case var c when SimpleSafe.Contains(c):
                return true;
            case "find":
                return !command.Any(arg => UnsafeFindOptions.Contains(arg));
            case "git":
                return command.Count > 1 && new[]{"branch","status","log","diff","show"}.Contains(command[1]);
            case "cargo":
                return command.Count > 1 && command[1] == "check";
            case "sed":
                return command.Count == 4 && command[1] == "-n" && IsValidSedNArg(command[2]) && !string.IsNullOrEmpty(command[3]);
            default:
                return false;
        }
    }

    public static bool IsKnownSafeCommand(IReadOnlyList<string> command)
    {
        if (IsSafeToCallWithExec(command))
            return true;
        if (command.Count == 3 && command[0] == "bash" && command[1] == "-lc")
        {
            var parsed = TryParseSingleWordOnlyCommand(command[2]);
            return parsed != null && IsSafeToCallWithExec(parsed);
        }
        return false;
    }

    private static List<string>? TryParseSingleWordOnlyCommand(string script)
    {
        var tokens = new List<string>();
        var sb = new StringBuilder();
        bool inSingle = false, inDouble = false;
        for (int i = 0; i < script.Length; i++)
        {
            char ch = script[i];
            if (inSingle)
            {
                if (ch == '\'') { tokens.Add(sb.ToString()); sb.Clear(); inSingle = false; }
                else sb.Append(ch);
            }
            else if (inDouble)
            {
                if (ch == '"') { tokens.Add(sb.ToString()); sb.Clear(); inDouble = false; }
                else sb.Append(ch);
            }
            else if (char.IsWhiteSpace(ch))
            {
                if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
            }
            else if (ch == '\'' )
            {
                if (sb.Length > 0) return null; // mixing quotes
                inSingle = true;
            }
            else if (ch == '"')
            {
                if (sb.Length > 0) return null;
                inDouble = true;
            }
            else if (ch == '|' || ch == '&' || ch == ';')
            {
                return null;
            }
            else
            {
                sb.Append(ch);
            }
        }
        if (inSingle || inDouble) return null;
        if (sb.Length > 0) tokens.Add(sb.ToString());
        return tokens.Count > 0 ? tokens : null;
    }
}

