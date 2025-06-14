namespace CodexCli.Config;

public class ConfigOverrides
{
    public Dictionary<string,string> Overrides { get; } = new();

    public static ConfigOverrides Parse(IEnumerable<string> pairs)
    {
        var overrides = new ConfigOverrides();
        foreach (var pair in pairs)
        {
            var idx = pair.IndexOf('=');
            if (idx <= 0) continue;
            var key = pair.Substring(0, idx);
            var val = pair[(idx + 1)..];
            overrides.Overrides[key] = val;
        }
        return overrides;
    }
}
