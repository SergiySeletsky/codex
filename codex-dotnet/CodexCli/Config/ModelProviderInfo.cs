using System.Collections.Generic;

namespace CodexCli.Config;

public enum WireApi
{
    Responses,
    Chat,
}

public class ModelProviderInfo
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string? EnvKey { get; set; }
    public WireApi WireApi { get; set; } = WireApi.Responses;

    public static Dictionary<string, ModelProviderInfo> BuiltIns { get; } = new()
    {
        ["openai"] = new ModelProviderInfo
        {
            Name = "OpenAI",
            BaseUrl = "https://api.openai.com/v1",
            EnvKey = "OPENAI_API_KEY",
            WireApi = WireApi.Responses,
        },
        ["openrouter"] = new ModelProviderInfo
        {
            Name = "OpenRouter",
            BaseUrl = "https://openrouter.ai/api/v1",
            EnvKey = "OPENROUTER_API_KEY",
            WireApi = WireApi.Chat,
        },
    };
}
