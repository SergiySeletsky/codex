using System.Text;

namespace CodexCli.Interactive;

/// <summary>
/// Parses common ANSI key escape sequences emitted by terminals. Mirrors the
/// Rust event decoding in codex-rs/tui/src/app.rs with additional sequences now
/// ported (done).
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

        if (_buf.Length == 2 && ch != '[' && ch != 'O')
        {
            _parsing = false;
            return true;
        }

        if (char.IsLetter(ch) || ch == '~')
        {
            _parsing = false;
            var seq = _buf.ToString();
            key = seq switch
            {
                "\u001b[A" => new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false),
                "\u001b[B" => new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false),
                "\u001b[C" => new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false),
                "\u001b[D" => new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false),
                "\u001b[H" or "\u001b[1~" or "\u001b[7~" or "\u001bOH" => new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false),
                "\u001b[F" or "\u001b[4~" or "\u001b[8~" or "\u001bOF" => new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false),
                "\u001b[5~" => new ConsoleKeyInfo('\0', ConsoleKey.PageUp, false, false, false),
                "\u001b[6~" => new ConsoleKeyInfo('\0', ConsoleKey.PageDown, false, false, false),
                "\u001b[3~" => new ConsoleKeyInfo('\0', ConsoleKey.Delete, false, false, false),
                _ => default
            };
            return key.Key != 0;
        }

        if (_buf.Length > 16)
            _parsing = false;

        return true;
    }
}
