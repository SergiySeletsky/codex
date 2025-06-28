using System.Text.Json;
using System.Collections.Generic;
using System.Threading;

// Ported from codex-rs/mcp-server/src/codex_tool_runner.rs (simplified)
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

        Func<string, OpenAIClient, string, CancellationToken, IAsyncEnumerable<Event>>? agent = null;
        if (providerId == "mock")
            agent = (p, c, m, t) => MockCodexAgent.RunAsync(p, Array.Empty<string>(), null, t);

        var (stream, first, _ctrlC) = await CodexWrapper.InitCodexAsync(param.Prompt, client, param.Model ?? "gpt-3.5-turbo", agent);
        emit(first);
        await foreach (var ev in stream)
            emit(ev);

        return new CallToolResult(
            new List<JsonElement> { JsonSerializer.SerializeToElement("codex done") },
            false);
    }

    /// <summary>
    /// Ported from codex-rs/mcp-server/src/codex_tool_config.rs
    /// `create_tool_for_codex_tool_call_param` (simplified).
    /// Returns a minimal Tool descriptor used by the MCP client tests.
    /// </summary>
    public static Tool CreateTool()
    {
        var schema = new ToolInputSchema(null, null, "object");
        return new Tool(
            "codex",
            schema,
            "Run a Codex session.",
            null);
    }
}
