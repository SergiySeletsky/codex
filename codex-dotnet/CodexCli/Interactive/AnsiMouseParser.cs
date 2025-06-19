// Rust counterpart in codex-rs/tui/src/app.rs (mouse wheel parser done)
using System.Text;

namespace CodexCli.Interactive;

/// <summary>
/// Parses xterm mouse wheel escape sequences and dispatches scroll events.
/// Mirrors scroll detection logic in codex-rs/tui/src/app.rs (done).
/// </summary>
public sealed class AnsiMouseParser
{
    private readonly ScrollEventHelper _helper;
    private readonly StringBuilder _buf = new();
    private bool _parsing;

    public AnsiMouseParser(ScrollEventHelper helper)
    {
        _helper = helper;
    }

    /// <summary>
    /// Processes a single input character. Returns true when the
    /// character was part of a recognized mouse sequence and consumed.
    /// </summary>
    public bool ProcessChar(char ch)
    {
        if (!_parsing)
        {
            if (ch == '\u001b')
            {
                _parsing = true;
                _buf.Clear();
                _buf.Append(ch);
                return true;
            }
            return false;
        }

        _buf.Append(ch);

        if (_buf.Length == 2 && ch != '[')
        {
            _parsing = false;
            return true;
        }

        if (ch == 'M' || ch == 'm')
        {
            var seq = _buf.ToString();
            _parsing = false;
            if (seq.StartsWith("\u001b[<64"))
                _helper.ScrollUp();
            else if (seq.StartsWith("\u001b[<65"))
                _helper.ScrollDown();
            return true;
        }

        // avoid unbounded growth
        if (_buf.Length > 16)
            _parsing = false;

        return true;
    }
}
