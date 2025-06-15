namespace CodexCli.Util;

public static class IsSafeCommand
{
    public static bool Check(IReadOnlyList<string> command)
    {
        if (command.Count == 0) return false;
        var cmd0 = command[0];
        return cmd0 switch
        {
            "cat" or "cd" or "echo" or "grep" or "head" or "ls" or "pwd" or "rg" or "tail" or "wc" or "which" => true,
            "find" => !command.Skip(1).Any(a => new[]{"-exec","-execdir","-ok","-okdir","-delete","-fls","-fprint","-fprint0","-fprintf"}.Contains(a)),
            "git" => command.Count > 1 && new[]{"branch","status","log","diff","show"}.Contains(command[1]),
            "cargo" => command.Count > 1 && command[1] == "check",
            "sed" => command.Count == 4 && command[1] == "-n" && IsValidSedArg(command[2]) && !string.IsNullOrEmpty(command[3]),
            _ => false
        };
    }

    private static bool IsValidSedArg(string? arg)
    {
        if (arg == null) return false;
        return System.Text.RegularExpressions.Regex.IsMatch(arg, "^[0-9]+(,[0-9]+)?p$");
    }
}
