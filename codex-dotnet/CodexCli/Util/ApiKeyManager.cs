using System.Text.Json;
using CodexCli.Config;

namespace CodexCli.Util;

public static class ApiKeyManager
{
    public const string DefaultEnvKey = "OPENAI_API_KEY";
    private static readonly string AuthFile = Path.Combine(EnvUtils.FindCodexHome(), "auth.json");

    public static void SaveKey(string provider, string key)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(AuthFile)!);
        Dictionary<string,string> map = new();
        if (File.Exists(AuthFile))
        {
            try
            {
                map = JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(AuthFile)) ?? new();
            }
            catch { }
        }
        map[provider] = key.Trim();
        File.WriteAllText(AuthFile, JsonSerializer.Serialize(map));
    }

    public static string? GetKey(ModelProviderInfo provider)
    {
        var envVar = provider.EnvKey ?? DefaultEnvKey;
        var env = Environment.GetEnvironmentVariable(envVar);
        if (!string.IsNullOrEmpty(env)) return env;
        if (File.Exists(AuthFile))
        {
            try
            {
                var map = JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(AuthFile));
                if (map != null && map.TryGetValue(provider.EnvKey ?? provider.Name.ToUpperInvariant()+"_API_KEY", out var val))
                    return val;
            }
            catch { }
        }
        return null;
    }

    public static string? LoadDefaultKey()
        => Environment.GetEnvironmentVariable(DefaultEnvKey);
}
