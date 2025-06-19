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
                        if (TryPngDimensions(bytes, out var w, out var h) || TryJpegDimensions(bytes, out w, out h))
                            return $"<image {w}x{h}>";
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
            Span<byte> buf = stackalloc byte[256];
            int read = fs.Read(buf);
            var bytes = buf.Slice(0, read).ToArray();
            if (TryPngDimensions(bytes, out var w, out var h) || TryJpegDimensions(bytes, out w, out h))
                return $"<image {w}x{h}>";
        }
        catch (Exception) { }

        return "<image>";
    }

    private static bool TryPngDimensions(ReadOnlySpan<byte> bytes, out int w, out int h)
    {
        w = h = 0;
        if (bytes.Length > 24 && bytes[12] == (byte)'I' && bytes[13] == (byte)'H' && bytes[14] == (byte)'D' && bytes[15] == (byte)'R')
        {
            w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
            return true;
        }
        return false;
    }

    private static bool TryJpegDimensions(ReadOnlySpan<byte> bytes, out int w, out int h)
    {
        w = h = 0;
        if (bytes.Length < 4 || bytes[0] != 0xFF || bytes[1] != 0xD8)
            return false;
        int i = 2;
        while (i + 9 < bytes.Length)
        {
            while (i < bytes.Length && bytes[i] != 0xFF)
                i++;
            if (i + 9 >= bytes.Length)
                break;
            byte marker = bytes[i + 1];
            int len = (bytes[i + 2] << 8) | bytes[i + 3];
            if (marker == 0xC0 || marker == 0xC2)
            {
                if (i + 7 >= bytes.Length) return false;
                h = (bytes[i + 5] << 8) | bytes[i + 6];
                w = (bytes[i + 7] << 8) | bytes[i + 8];
                return true;
            }
            i += 2 + len;
        }
        return false;
    }
}
