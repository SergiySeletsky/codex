using System.Text.Json;
using CodexCli.Protocol;

namespace CodexCli.Util;

/// <summary>
/// Minimal placeholder implementation of the Codex tool-call used by the MCP server.
/// It emits a few fake events and returns a simple result so that higher level
/// components can be exercised without the real Codex core.
/// </summary>
public static class CodexToolRunner
{
    public static Task<CallToolResult> RunCodexToolSessionAsync(
        CodexToolCallParam param,
        Action<Event> emit)
    {
        emit(new SessionConfiguredEvent(Guid.NewGuid().ToString(), "session", param.Model ?? "model"));
        emit(new AgentMessageEvent(Guid.NewGuid().ToString(), $"Echo: {param.Prompt}"));
        emit(new TaskCompleteEvent(Guid.NewGuid().ToString(), "done"));

        var result = new CallToolResult(
            new List<JsonElement> { JsonSerializer.SerializeToElement("codex done") },
            false);
        return Task.FromResult(result);
    }
}

public record CodexToolCallParam(
    string Prompt,
    string? Model = null,
    string? Profile = null,
    string? Cwd = null,
    string? ApprovalPolicy = null,
    IReadOnlyList<string>? SandboxPermissions = null,
    Dictionary<string, JsonElement>? Config = null);
