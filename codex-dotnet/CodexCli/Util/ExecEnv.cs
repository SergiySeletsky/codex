using CodexCli.Config;
using System.Collections;

namespace CodexCli.Util;

public static class ExecEnv
{
    public static Dictionary<string,string> Create(ShellEnvironmentPolicy policy)
    {
        IEnumerable<KeyValuePair<string,string>> vars = Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>().ToDictionary(d => (string)d.Key, d => (string)d.Value);

        Dictionary<string,string> map = policy.Inherit switch
        {
            ShellEnvironmentPolicyInherit.All => new Dictionary<string,string>(vars),
            ShellEnvironmentPolicyInherit.None => new Dictionary<string,string>(),
            _ => new Dictionary<string,string>(vars.Where(kv => CoreVars.Contains(kv.Key)))
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
