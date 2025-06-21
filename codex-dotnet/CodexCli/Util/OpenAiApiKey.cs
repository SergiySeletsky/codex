namespace CodexCli.Util;

/// <summary>
/// Simplified port of codex-rs/core/src/openai_api_key.rs (done).
/// Provides cached access to the OPENAI_API_KEY environment variable.
/// </summary>
public static class OpenAiApiKey
{
    public const string EnvVar = "OPENAI_API_KEY";

    private static string? _key = Environment.GetEnvironmentVariable(EnvVar);

    public static string? Get()
    {
        return string.IsNullOrEmpty(_key) ? null : _key;
    }

    public static void Set(string value)
    {
        if (!string.IsNullOrEmpty(value))
            _key = value;
    }
}
