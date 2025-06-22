using CodexCli.Config;
using System.Collections;

/// <summary>
/// Simplified port of codex-rs/core/src/exec_env.rs (done).
/// </summary>

namespace CodexCli.Util;

public static class ExecEnv
{
    public static Dictionary<string,string> Create(ShellEnvironmentPolicy policy)
    {
        var vars = new List<KeyValuePair<string,string>>();
        foreach (DictionaryEntry de in Environment.GetEnvironmentVariables())
        {
            vars.Add(new KeyValuePair<string,string>((string)de.Key, (string)de.Value));
        }
        return CreateFrom(vars, policy);
    }

    public static Dictionary<string,string> CreateFrom(IEnumerable<KeyValuePair<string,string>> vars, ShellEnvironmentPolicy policy)
    {
        var varsMap = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in vars)
            varsMap[kv.Key] = kv.Value;

        Dictionary<string,string> map = policy.Inherit switch
        {
            ShellEnvironmentPolicyInherit.All => new Dictionary<string,string>(varsMap, StringComparer.OrdinalIgnoreCase),
            ShellEnvironmentPolicyInherit.None => new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase),
            _ => new Dictionary<string,string>(varsMap.Where(kv => CoreVars.Contains(kv.Key)), StringComparer.OrdinalIgnoreCase)
        };

        bool Matches(string name, IEnumerable<EnvironmentVariablePattern> patterns) =>
            patterns.Any(p => p.Matches(name));

        if (!policy.IgnoreDefaultExcludes)
        {
            var defaults = new[]
            {
                EnvironmentVariablePattern.CaseInsensitive("*KEY*"),
                EnvironmentVariablePattern.CaseInsensitive("*SECRET*"),
                EnvironmentVariablePattern.CaseInsensitive("*TOKEN*")
            };
            foreach (var k in map.Keys.ToList())
                if (Matches(k, defaults))
                    map.Remove(k);
        }

        if (policy.Exclude.Count > 0)
        {
            foreach (var k in map.Keys.ToList())
                if (Matches(k, policy.Exclude))
                    map.Remove(k);
        }

        foreach (var (k,v) in policy.Set)
            map[k] = v;

        if (policy.IncludeOnly.Count > 0)
        {
            foreach (var k in map.Keys.ToList())
                if (!Matches(k, policy.IncludeOnly))
                    map.Remove(k);
        }

        return map;
    }

    private static readonly HashSet<string> CoreVars = new(
        new[]{"HOME","LOGNAME","PATH","SHELL","USER","USERNAME","TMPDIR","TEMP","TMP"},
        StringComparer.OrdinalIgnoreCase);
}
