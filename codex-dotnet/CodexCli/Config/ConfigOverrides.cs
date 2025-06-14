using Tomlyn;

namespace CodexCli.Config;

public class ConfigOverrides
{
    public Dictionary<string, object?> Overrides { get; } = new();

    public static ConfigOverrides Parse(IEnumerable<string> pairs)
    {
        var overrides = new ConfigOverrides();
        foreach (var pair in pairs)
        {
            var idx = pair.IndexOf('=');
            if (idx <= 0) continue;
            var key = pair.Substring(0, idx).Trim();
            var valueStr = pair[(idx + 1)..].Trim();
            var wrapped = $"_x_ = {valueStr}";
            try
            {
                var model = Toml.ToModel(wrapped);
                if (model.TryGetValue("_x_", out var val))
                    overrides.Overrides[key] = val;
                else
                    overrides.Overrides[key] = valueStr;
            }
            catch
            {
                overrides.Overrides[key] = valueStr;
            }
        }
        return overrides;
    }
}
