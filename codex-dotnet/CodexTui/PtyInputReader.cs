using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodexCli.Interactive;

namespace CodexTui;

/// <summary>
/// Simple PTY input reader running on a background task. Mirrors the
/// crossterm event loop in codex-rs/tui/src/app.rs (done, paste capped
/// and flushed on dispose with timeout handling using async reads).
/// </summary>
public sealed class PtyInputReader : IDisposable
{
    private readonly TextReader _reader;
    private readonly AnsiMouseParser _mouseParser;
    private readonly AnsiKeyParser _keyParser = new();
    private readonly StringBuilder _pasteBuf = new();
    /// <summary>Maximum characters buffered while parsing a paste.</summary>
    public const int MaxPasteLength = 4096;
    private bool _detectPaste;
    private bool _inPaste;
    private readonly ConcurrentQueue<ConsoleKeyInfo> _keys = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _task;
    private DateTime _lastInput = DateTime.UtcNow;

    /// <summary>Milliseconds to wait before flushing a partial paste.</summary>
    public const int PartialPasteTimeoutMs = 250;

    public PtyInputReader(TextReader reader, AnsiMouseParser parser)
    {
        _reader = reader;
        _mouseParser = parser;
        _task = Task.Run(ReadLoopAsync);
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            var buffer = new char[1];
            while (!_cts.IsCancellationRequested)
            {
                int read = await _reader.ReadAsync(buffer.AsMemory(0, 1));
                if (read == 0)
                    break;
                int ch = buffer[0];
                if (ch == -1)
                    break;
                _lastInput = DateTime.UtcNow;
                char c = (char)ch;

                if (_inPaste)
                {
                    _pasteBuf.Append(c);
                    if (_pasteBuf.Length > MaxPasteLength)
                    {
                        foreach (var pc in _pasteBuf.ToString())
                            HandleChar(pc);
                        _pasteBuf.Clear();
                        _inPaste = false;
                        continue;
                    }
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
                    if (_pasteBuf.Length > MaxPasteLength)
                    {
                        HandleChar('\u001b');
                        foreach (var pc in _pasteBuf.ToString())
                            HandleChar(pc);
                        _pasteBuf.Clear();
                        _detectPaste = false;
                        continue;
                    }
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

    public bool TryRead(out ConsoleKeyInfo key)
    {
        if (_keys.IsEmpty && (_inPaste || _detectPaste) &&
            (DateTime.UtcNow - _lastInput).TotalMilliseconds > PartialPasteTimeoutMs)
        {
            FlushPartialPaste();
        }
        return _keys.TryDequeue(out key);
    }

    public bool HasPendingKeys
    {
        get
        {
            if (_keys.IsEmpty && (_inPaste || _detectPaste) &&
                (DateTime.UtcNow - _lastInput).TotalMilliseconds > PartialPasteTimeoutMs)
                FlushPartialPaste();
            return !_keys.IsEmpty;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _task.Wait(100); } catch { }
        FlushPartialPaste();
    }

    private void FlushPartialPaste()
    {
        if (_inPaste || _detectPaste)
        {
            if (_detectPaste)
                HandleChar('\u001b');
            foreach (var ch in _pasteBuf.ToString())
                HandleChar(ch);
            _pasteBuf.Clear();
            _inPaste = false;
            _detectPaste = false;
            _lastInput = DateTime.UtcNow;
        }
    }
}
