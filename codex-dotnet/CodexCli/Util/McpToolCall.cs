using System.Text.Json;
using CodexCli.Protocol;
using CodexCli.Models;

namespace CodexCli.Util;

/// <summary>
/// Port of codex-rs/core/src/mcp_tool_call.rs (done).
/// </summary>
public static class McpToolCall
{
    public static async Task<ResponseInputItem> HandleMcpToolCallAsync(McpClient client, string callId, string toolName, JsonElement? args, int timeoutSeconds = 10)
    {
        try
        {
            var result = await client.CallToolAsync(toolName, args, timeoutSeconds);
            var json = JsonSerializer.Serialize(result);
            return new McpToolCallOutputInputItem(callId, json);
        }
        catch (Exception ex)
        {
            return new McpToolCallOutputInputItem(callId, $"error: {ex.Message}");
        }
    }
}
