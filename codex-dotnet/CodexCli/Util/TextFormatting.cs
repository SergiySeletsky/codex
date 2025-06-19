using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CodexCli.Util;

/// <summary>
/// Helpers for formatting tool output and truncating text.
/// Mirrors codex-rs/tui/src/text_formatting.rs (done).
/// </summary>
public static class TextFormatting
{
    // Rust: format_and_truncate_tool_result
    public static string FormatAndTruncateToolResult(string text, int maxLines, int lineWidth)
    {
        int maxGraphemes = Math.Max(0, maxLines * lineWidth - maxLines);
        var formatted = FormatJsonCompact(text) ?? text;
        return TruncateText(formatted, maxGraphemes);
    }

    // Rust: format_json_compact
    public static string? FormatJsonCompact(string text)
    {
        try
        {
            using var doc = JsonDocument.Parse(text);
            var pretty = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            var sb = new StringBuilder();
            bool inString = false;
            bool escapeNext = false;
            for (int i = 0; i < pretty.Length; i++)
            {
                char ch = pretty[i];
                if (inString)
                {
                    sb.Append(ch);
                    if (escapeNext)
                        escapeNext = false;
                    else if (ch == '\\')
                        escapeNext = true;
                    else if (ch == '"')
                        inString = false;
                    continue;
                }

                switch (ch)
                {
                    case '"':
                        inString = true;
                        sb.Append(ch);
                        break;
                    case '\n':
                    case '\r':
                        break;
                    case ' ':
                    case '\t':
                        if (sb.Length > 0)
                        {
                            char last = sb[^1];
                            char next = i + 1 < pretty.Length ? pretty[i + 1] : '\0';
                            if ((last == ':' || last == ',') && next != '}' && next != ']')
                                sb.Append(' ');
                        }
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Rust: truncate_text
    public static string TruncateText(string text, int maxGraphemes)
    {
        if (maxGraphemes <= 0) return string.Empty;
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        var indices = new List<int>();
        int byteIndex = 0;
        while (enumerator.MoveNext())
        {
            indices.Add(byteIndex);
            byteIndex += ((string)enumerator.Current!).Length;
        }
        // append end index
        indices.Add(text.Length);

        if (indices.Count - 1 <= maxGraphemes)
            return text;

        if (maxGraphemes >= 3)
        {
            int cut = indices[maxGraphemes - 3];
            return text.Substring(0, cut) + "...";
        }
        else
        {
            int cut = indices[maxGraphemes];
            return text.Substring(0, cut);
        }
    }
}
