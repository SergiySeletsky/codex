using System.Text.Json;
using CodexCli.Protocol;
using CodexCli.Models;
using System.Threading.Channels;
using System;

namespace CodexCli.Util;

/// <summary>
/// Port of codex-rs/core/src/mcp_tool_call.rs (done).
/// </summary>
public static class McpToolCall
{
    /// <summary>
    /// Ported from codex-rs/core/src/mcp_tool_call.rs `handle_mcp_tool_call` (done).
    /// Emits begin and end events around the tool call and returns the result payload.
    /// </summary>
    public static async Task<ResponseInputItem> HandleMcpToolCallAsync(
        McpConnectionManager manager,
        ChannelWriter<Event> events,
        string subId,
        string callId,
        string server,
        string toolName,
        string arguments,
        TimeSpan? timeout = null)
    {
        JsonElement? args = null;
        if (!string.IsNullOrWhiteSpace(arguments))
        {
            try
            {
                args = JsonSerializer.Deserialize<JsonElement>(arguments);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"failed to parse tool call arguments: {e.Message}");
                var errPayload = new FunctionCallOutputPayload($"err: {e.Message}", false);
                return new FunctionCallOutputInputItem(callId, errPayload);
            }
        }

        var begin = new McpToolCallBeginEvent(callId, server, toolName, args?.GetRawText());
        await Codex.SendEventAsync(events, begin);

        string resultJson;
        bool success;
        try
        {
            var result = await Codex.CallToolAsync(manager, server, toolName, args, timeout);
            resultJson = JsonSerializer.Serialize(result);
            success = result.IsError != true;
        }
        catch (Exception ex)
        {
            resultJson = $"tool call error: {ex.Message}";
            success = false;
        }

        var endEv = new McpToolCallEndEvent(callId, success, resultJson);
        await Codex.SendEventAsync(events, endEv);

        return new McpToolCallOutputInputItem(callId, resultJson);
    }
}
