// Ported from codex-rs/execpolicy/src/lib.rs default policy loader
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace CodexCli.Util;

public class ExecPolicy
{
    private readonly HashSet<string> _allowed = new();
    private readonly Dictionary<string, string> _forbidden = new();
    private readonly Dictionary<string, HashSet<string>> _options = new();

    public static ExecPolicy LoadDefault()
    {
        var env = Environment.GetEnvironmentVariable("CODEX_EXEC_POLICY_PATH");
        var baseDir = AppContext.BaseDirectory;
        var path = env ?? Path.Combine(baseDir, "..", "..", "..", "..", "..", "codex-rs", "execpolicy", "src", "default.policy");
        path = Path.GetFullPath(path);
        var text = File.ReadAllText(path);
        var policy = new ExecPolicy();
        int idx = 0;
        while ((idx = text.IndexOf("define_program(", idx)) >= 0)
        {
            int start = idx + "define_program(".Length;
            int depth = 1;
            int end = start;
            while (end < text.Length && depth > 0)
            {
                char ch = text[end];
                if (ch == '(') depth++;
                else if (ch == ')') depth--;
                end++;
            }
            var body = text.Substring(start, end - start - 1);
            var m = Regex.Match(body, "program=\"(?<p>[^\"]+)\"");
            if (!m.Success) continue;
            var prog = m.Groups["p"].Value;
            policy._allowed.Add(prog);
            var opts = new HashSet<string>();
            foreach (Match fm in Regex.Matches(body, @"flag\(""(?<f>[^""]+)""\)"))
                opts.Add(fm.Groups["f"].Value);
            foreach (Match om in Regex.Matches(body, @"opt\(""(?<f>[^""]+)"""))
                opts.Add(om.Groups["f"].Value);
            policy._options[prog] = opts;
            idx = end;
        }
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

    public bool VerifyCommand(string program, IEnumerable<string> args)
    {
        program = Path.GetFileName(program);
        if (!_allowed.Contains(program)) return false;
        if (!_options.TryGetValue(program, out var opts)) return true;
        foreach (var arg in args)
        {
            if (!arg.StartsWith("-")) continue;
            var key = arg.Contains('=') ? arg.Split('=')[0] : arg;
            if (!opts.Contains(key)) return false;
        }
        return true;
    }

    public bool IsForbidden(string program) => _forbidden.ContainsKey(Path.GetFileName(program));

    public string? GetReason(string program)
    {
        _forbidden.TryGetValue(Path.GetFileName(program), out var reason);
        return reason;
    }
}
