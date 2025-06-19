using System.Text;

namespace CodexCli.Interactive;

/// <summary>
/// Parses arrow key escape sequences emitted by terminals. Mirrors the
/// Rust logic in codex-rs/tui/src/app.rs (done).
/// </summary>
public sealed class AnsiKeyParser
{
    private readonly StringBuilder _buf = new();
    private bool _parsing;

    /// <summary>
    /// Process a single input character. When a complete arrow sequence is
    /// recognized, returns true and sets <paramref name="key"/>.
    /// Returns true when the character was consumed as part of a sequence.
    /// </summary>
    public bool ProcessChar(char ch, out ConsoleKeyInfo key)
    {
        key = default;
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

        if (_buf.Length == 3)
        {
            _parsing = false;
            key = ch switch
            {
                'A' => new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false),
                'B' => new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false),
                'C' => new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false),
                'D' => new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false),
                _ => default
            };
            return key.Key != 0;
        }

        if (_buf.Length > 16)
            _parsing = false;

        return true;
    }
}
