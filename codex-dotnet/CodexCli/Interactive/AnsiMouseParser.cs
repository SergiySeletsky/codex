using System.Text;

namespace CodexCli.Interactive;

/// <summary>
/// Parses xterm mouse wheel escape sequences and dispatches scroll events.
/// Mirrors scroll event detection in codex-rs/tui/src/app.rs (done).
/// </summary>
public sealed class AnsiMouseParser
{
    private StringBuilder _buf = new();
    private bool _parsing;

    /// <summary>
    /// Processes a single input character.
    /// Returns scroll delta when a sequence completes, otherwise null.
    /// </summary>
    public int? ProcessChar(char ch)
    {
        if (!_parsing)
        {
            if (ch == '\u001b')
            {
                _parsing = true;
                _buf.Clear();
                _buf.Append(ch);
                return null;
            }
            return null;
        }
        _buf.Append(ch);
        if (_buf.Length == 2 && ch != '[')
        {
            _parsing = false;
            return null;
        }
        if (ch == 'M' || ch == 'm')
        {
            var seq = _buf.ToString();
            _parsing = false;
            if (seq.StartsWith("\u001b[<64")) return -1;
            if (seq.StartsWith("\u001b[<65")) return 1;
            return null;
        }
        // avoid unbounded growth
        if (_buf.Length > 16)
            _parsing = false;
        return null;
    }
}
