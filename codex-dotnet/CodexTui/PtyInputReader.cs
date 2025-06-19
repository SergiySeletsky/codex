using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CodexCli.Interactive;

namespace CodexTui;

/// <summary>
/// Simple PTY input reader running on a background thread. Mirrors the
/// crossterm event loop in codex-rs/tui/src/app.rs (pending).
/// </summary>
public sealed class PtyInputReader : IDisposable
{
    private readonly TextReader _reader;
    private readonly AnsiMouseParser _mouseParser;
    private readonly AnsiKeyParser _keyParser = new();
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
                if (_mouseParser.ProcessChar(c))
                    continue;
                if (_keyParser.ProcessChar(c, out var k))
                {
                    if (k.Key != 0)
                        _keys.Enqueue(k);
                    continue;
                }
                _keys.Enqueue(new ConsoleKeyInfo(c, ConsoleKey.NoName, false, false, false));
            }
        }
        catch { }
    }

    public bool TryRead(out ConsoleKeyInfo key) => _keys.TryDequeue(out key);

    public void Dispose()
    {
        _cts.Cancel();
        try { _thread.Join(100); } catch { }
    }
}
