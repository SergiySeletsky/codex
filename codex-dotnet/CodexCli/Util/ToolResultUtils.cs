using System.Text.Json;

namespace CodexCli.Util;

/// <summary>
/// Helpers for parsing tool call results.
/// Mirrors logic from codex-rs/tui/src/history_cell.rs::try_new_completed_mcp_tool_call_with_image_output (done)
/// </summary>
public static class ToolResultUtils
{
    public static bool HasImageOutput(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in content.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var type) && type.GetString() == "image")
                        return true;
                }
            }
        }
        catch (JsonException) { }
        return false;
    }
}
