using System.Text.RegularExpressions;

namespace CodexCli.Util;

public class ExecPolicy
{
    private readonly HashSet<string> _allowed = new();
    private readonly Dictionary<string, string> _forbidden = new();

    public static ExecPolicy LoadDefault()
    {
        var baseDir = AppContext.BaseDirectory;
        var path = Path.Combine(baseDir, "..", "..", "..", "..", "..", "codex-rs", "execpolicy", "src", "default.policy");
        path = Path.GetFullPath(path);
        var text = File.ReadAllText(path);
        var policy = new ExecPolicy();
        foreach (Match m in Regex.Matches(text, "program=\"(?<p>[^\"]+)\""))
            policy._allowed.Add(m.Groups["p"].Value);
        policy._forbidden["rm"] = "dangerous";
        policy._forbidden["reboot"] = "dangerous";
        policy._forbidden["shutdown"] = "dangerous";
        return policy;
    }

    public bool IsAllowed(string program)
    {
        program = Path.GetFileName(program);
        return _allowed.Contains(program);
    }

    public bool IsForbidden(string program) => _forbidden.ContainsKey(Path.GetFileName(program));

    public string? GetReason(string program)
    {
        _forbidden.TryGetValue(Path.GetFileName(program), out var reason);
        return reason;
    }
}
