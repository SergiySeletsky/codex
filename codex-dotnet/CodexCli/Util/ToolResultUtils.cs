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

    public static string FormatImageInfo(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in content.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var type) && type.GetString() == "image" &&
                        item.TryGetProperty("data", out var data))
                    {
                        var bytes = Convert.FromBase64String(data.GetString() ?? "");
                        if (bytes.Length > 24 && bytes[12] == (byte)'I' && bytes[13] == (byte)'H' && bytes[14] == (byte)'D' && bytes[15] == (byte)'R')
                        {
                            int w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                            int h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                            return $"<image {w}x{h}>";
                        }
                        return "<image output>";
                    }
                }
            }
        }
        catch (Exception) { }

        return "<image output>";
    }

    public static string FormatImageInfoFromFile(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            Span<byte> buf = stackalloc byte[24];
            int read = fs.Read(buf);
            if (read >= 24 && buf[12] == (byte)'I' && buf[13] == (byte)'H' && buf[14] == (byte)'D' && buf[15] == (byte)'R')
            {
                int w = (buf[16] << 24) | (buf[17] << 16) | (buf[18] << 8) | buf[19];
                int h = (buf[20] << 24) | (buf[21] << 16) | (buf[22] << 8) | buf[23];
                return $"<image {w}x{h}>";
            }
        }
        catch (Exception) { }

        return "<image>";
    }
}
