using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodexCli.Interactive;

namespace CodexTui;

/// <summary>
/// Simple PTY input reader running on a background thread. Mirrors the
/// crossterm event loop in codex-rs/tui/src/app.rs (done).
/// </summary>
public sealed class PtyInputReader : IDisposable
{
    private readonly TextReader _reader;
    private readonly AnsiMouseParser _mouseParser;
    private readonly AnsiKeyParser _keyParser = new();
    private readonly StringBuilder _pasteBuf = new();
    private bool _detectPaste;
    private bool _inPaste;
    private readonly ConcurrentQueue<ConsoleKeyInfo> _keys = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Thread _thread;

    public PtyInputReader(TextReader reader, AnsiMouseParser parser)
    {
        _reader = reader;
        _mouseParser = parser;
        _thread = new Thread(ReadLoop) { IsBackground = true };
        _thread.Start();
    }

    private void ReadLoop()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                int ch = _reader.Read();
                if (ch == -1)
                    break;
                char c = (char)ch;

                if (_inPaste)
                {
                    _pasteBuf.Append(c);
                    if (_pasteBuf.Length >= 6 && _pasteBuf.ToString().EndsWith("\u001b[201~"))
                    {
                        var text = _pasteBuf.ToString(0, _pasteBuf.Length - 6);
                        foreach (var pc in text)
                        {
                            if (pc == '\n' || pc == '\r')
                                _keys.Enqueue(new ConsoleKeyInfo('\n', ConsoleKey.Enter, true, false, false));
                            else
                                _keys.Enqueue(new ConsoleKeyInfo(pc, ConsoleKey.NoName, false, false, false));
                        }
                        _pasteBuf.Clear();
                        _inPaste = false;
                    }
                    continue;
                }

                if (_detectPaste)
                {
                    _pasteBuf.Append(c);
                    var str = _pasteBuf.ToString();
                    if ("[200~".StartsWith(str))
                    {
                        if (str == "[200~")
                        {
                            _detectPaste = false;
                            _inPaste = true;
                            _pasteBuf.Clear();
                        }
                        continue;
                    }
                    else
                    {
                        HandleChar('\u001b');
                        foreach (var pc in str)
                            HandleChar(pc);
                        _pasteBuf.Clear();
                        _detectPaste = false;
                        continue;
                    }
                }

                if (c == '\u001b')
                {
                    _detectPaste = true;
                    _pasteBuf.Clear();
                    continue;
                }

                HandleChar(c);
            }
        }
        catch { }
    }

    private void HandleChar(char c)
    {
        if (_mouseParser.ProcessChar(c))
            return;
        if (_keyParser.ProcessChar(c, out var k))
        {
            if (k.Key != 0)
                _keys.Enqueue(k);
            return;
        }
        _keys.Enqueue(new ConsoleKeyInfo(c, ConsoleKey.NoName, false, false, false));
    }

    public bool TryRead(out ConsoleKeyInfo key) => _keys.TryDequeue(out key);

    public void Dispose()
    {
        _cts.Cancel();
        try { _thread.Join(100); } catch { }
    }
}
