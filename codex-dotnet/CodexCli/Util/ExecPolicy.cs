using System.Text.RegularExpressions;

namespace CodexCli.Util;

public class ExecPolicy
{
    private readonly HashSet<string> _allowed = new();

    public static ExecPolicy LoadDefault()
    {
        var baseDir = AppContext.BaseDirectory;
        var path = Path.Combine(baseDir, "..", "..", "..", "..", "..", "codex-rs", "execpolicy", "src", "default.policy");
        path = Path.GetFullPath(path);
        var text = File.ReadAllText(path);
        var policy = new ExecPolicy();
        foreach (Match m in Regex.Matches(text, "program=\"(?<p>[^\"]+)\""))
            policy._allowed.Add(m.Groups["p"].Value);
        return policy;
    }

    public bool IsAllowed(string program)
    {
        program = Path.GetFileName(program);
        return _allowed.Contains(program);
    }
}
