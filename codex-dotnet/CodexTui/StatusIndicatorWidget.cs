using Spectre.Console;

namespace CodexTui;

/// <summary>
/// Minimal status indicator placeholder.
/// Mirrors codex-rs/tui/src/status_indicator_widget.rs (in progress).
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
            var frames = new[] { ".", "..", "..." };
            while (!_cts.Token.IsCancellationRequested)
            {
                AnsiConsole.MarkupLine($"[grey]{_text} {frames[idx]}[/]");
                idx = (idx + 1) % frames.Length;
                await Task.Delay(200);
            }
        });
    }

    public void UpdateText(string text)
    {
        _text = text.Replace('\n', ' ').Replace('\r', ' ');
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _task?.Wait(); } catch { }
    }
}
