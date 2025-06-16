using Spectre.Console;
using CodexCli.Util;

namespace CodexCli.Interactive;

/// <summary>
/// Animated status indicator mirroring the Rust version.
/// Updates a single console line with a three-dot animation and the
/// latest log text. Implemented as codex-dotnet counterpart of
/// codex-rs/tui/src/status_indicator_widget.rs (done).
/// </summary>
internal sealed class StatusIndicatorWidget : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private string _text = "waiting for logs…";
    private Task? _task;

    public void Start()
    {
        _task = Task.Run(async () =>
        {
            int idx = 0;
            const int DotCount = 3;
            while (!_cts.Token.IsCancellationRequested)
            {
                int phase = idx % (DotCount * 2 - 2);
                int active = phase < DotCount ? phase : (DotCount * 2 - 2) - phase;
                var clean = AnsiEscape.StripAnsi(_text);
                int maxWidth = Console.IsOutputRedirected ? 80 : Console.WindowWidth;
                string header = "Working [";
                for (int i = 0; i < DotCount; i++)
                    header += i == active ? "." : ".";
                header += $"] ";
                int available = Math.Max(0, maxWidth - header.Length);
                if (clean.Length > available)
                    clean = clean.Substring(0, available);
                var line = (header + clean).PadRight(maxWidth);
                Console.Write("\r" + line);
                idx++;
                await Task.Delay(200);
            }
            Console.WriteLine();
        });
    }

    public void UpdateText(string text)
    {
        _text = AnsiEscape.StripAnsi(text).Replace('\n', ' ').Replace('\r', ' ');
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _task?.Wait(); } catch { }
        Console.WriteLine();
    }
}
