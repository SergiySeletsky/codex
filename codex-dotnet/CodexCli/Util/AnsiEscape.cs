using System.Text.RegularExpressions;

namespace CodexCli.Util;

/// <summary>
/// Basic ANSI escape sequence helper mirroring codex-rs/ansi-escape crate.
/// </summary>
public static class AnsiEscape
{
    private static readonly Regex EscapeRegex = new(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);

    /// <summary>
    /// Remove ANSI escape sequences and return the first line only.
    /// </summary>
    public static string StripAnsi(string text)
    {
        var noCr = text.Replace("\r", string.Empty);
        var firstLine = noCr.Split('\n')[0];
        return EscapeRegex.Replace(firstLine, string.Empty);
    }
}
