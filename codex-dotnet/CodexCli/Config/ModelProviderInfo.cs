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
    public string? EnvKeyInstructions { get; set; }
    public WireApi WireApi { get; set; } = WireApi.Responses;

    public static Dictionary<string, ModelProviderInfo> BuiltIns { get; } = new()
    {
        ["openai"] = new ModelProviderInfo
        {
            Name = "OpenAI",
            BaseUrl = "https://api.openai.com/v1",
            EnvKey = "OPENAI_API_KEY",
            EnvKeyInstructions = "Create an API key (https://platform.openai.com) and export it as OPENAI_API_KEY",
            WireApi = WireApi.Responses,
        },
        ["openrouter"] = new ModelProviderInfo
        {
            Name = "OpenRouter",
            BaseUrl = "https://openrouter.ai/api/v1",
            EnvKey = "OPENROUTER_API_KEY",
            WireApi = WireApi.Chat,
        },
        ["gemini"] = new ModelProviderInfo
        {
            Name = "Gemini",
            BaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai",
            EnvKey = "GEMINI_API_KEY",
            WireApi = WireApi.Chat,
        },
        ["ollama"] = new ModelProviderInfo
        {
            Name = "Ollama",
            BaseUrl = "http://localhost:11434/v1",
            EnvKey = null,
            WireApi = WireApi.Chat,
        },
        ["mistral"] = new ModelProviderInfo
        {
            Name = "Mistral",
            BaseUrl = "https://api.mistral.ai/v1",
            EnvKey = "MISTRAL_API_KEY",
            WireApi = WireApi.Chat,
        },
        ["deepseek"] = new ModelProviderInfo
        {
            Name = "DeepSeek",
            BaseUrl = "https://api.deepseek.com",
            EnvKey = "DEEPSEEK_API_KEY",
            WireApi = WireApi.Chat,
        },
        ["xai"] = new ModelProviderInfo
        {
            Name = "xAI",
            BaseUrl = "https://api.x.ai/v1",
            EnvKey = "XAI_API_KEY",
            WireApi = WireApi.Chat,
        },
        ["groq"] = new ModelProviderInfo
        {
            Name = "Groq",
            BaseUrl = "https://api.groq.com/openai/v1",
            EnvKey = "GROQ_API_KEY",
            WireApi = WireApi.Chat,
        },
        ["perplexity"] = new ModelProviderInfo
        {
            Name = "Perplexity",
            BaseUrl = "https://api.perplexity.ai",
            EnvKey = "PERPLEXITY_API_KEY",
            WireApi = WireApi.Chat,
        },
        ["anthropic"] = new ModelProviderInfo
        {
            Name = "Anthropic",
            BaseUrl = "https://api.anthropic.com/v1",
            EnvKey = "ANTHROPIC_API_KEY",
            WireApi = WireApi.Chat,
        },
        ["mock"] = new ModelProviderInfo
        {
            Name = "Mock",
            BaseUrl = "http://localhost",
            EnvKey = null,
            WireApi = WireApi.Responses,
        },
    };
}
