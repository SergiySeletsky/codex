namespace CodexCli.Util;

public static class OpenAiKeyManager
{
    private static string? _cached;
    private const string EnvVar = "OPENAI_API_KEY";

    public static string? GetKey()
    {
        if (_cached != null) return _cached;
        var env = Environment.GetEnvironmentVariable(EnvVar);
        if (!string.IsNullOrEmpty(env))
        {
            _cached = env;
            return env;
        }
        var path = Path.Combine(EnvUtils.FindCodexHome(), "auth.json");
        if (File.Exists(path))
        {
            try
            {
                var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
                if (json.RootElement.TryGetProperty("OPENAI_API_KEY", out var val))
                {
                    _cached = val.GetString();
                    return _cached;
                }
            }
            catch { }
        }
        return null;
    }

    public static void SetKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        Environment.SetEnvironmentVariable(EnvVar, key);
        _cached = key;
    }
}
