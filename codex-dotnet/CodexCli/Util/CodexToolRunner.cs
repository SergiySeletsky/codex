using System.Text.Json;
using CodexCli.Protocol;
using CodexCli.Config;

namespace CodexCli.Util;

/// <summary>
/// Minimal placeholder implementation of the Codex tool-call used by the MCP server.
/// It emits a few fake events and returns a simple result so that higher level
/// components can be exercised without the real Codex core.
/// </summary>
public static class CodexToolRunner
{
    public static async Task<CallToolResult> RunCodexToolSessionAsync(
        CodexToolCallParam param,
        Action<Event> emit)
    {
        var providerId = param.Provider ?? "openai";
        var providerInfo = ModelProviderInfo.BuiltIns.TryGetValue(providerId, out var info)
            ? info
            : ModelProviderInfo.BuiltIns["openai"];
        var apiKey = ApiKeyManager.GetKey(providerInfo);
        var client = new OpenAIClient(apiKey, providerInfo.BaseUrl);

        await foreach (var ev in RealCodexAgent.RunAsync(param.Prompt, client, param.Model ?? "gpt-3.5-turbo", null, Array.Empty<string>()))
        {
            emit(ev);
        }

        return new CallToolResult(
            new List<JsonElement> { JsonSerializer.SerializeToElement("codex done") },
            false);
    }
}

public record CodexToolCallParam(
    string Prompt,
    string? Model = null,
    string? Profile = null,
    string? Cwd = null,
    string? ApprovalPolicy = null,
    IReadOnlyList<string>? SandboxPermissions = null,
    Dictionary<string, JsonElement>? Config = null,
    string? Provider = null);
