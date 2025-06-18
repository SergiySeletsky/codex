using CodexCli.Protocol;

namespace CodexCli.Interactive;

/// <summary>
/// Wrapper view for StatusIndicatorWidget.
/// Mirrors codex-rs/tui/src/bottom_pane/status_indicator_view.rs (done).
/// </summary>
internal class StatusIndicatorView : IBottomPaneView
{
    private readonly StatusIndicatorWidget _widget;
    private readonly int _height;

    public StatusIndicatorView(StatusIndicatorWidget widget, int height)
    {
        _widget = widget;
        _height = Math.Max(1, height);
    }

    public void HandleKeyEvent(ConsoleKeyInfo key, BottomPane pane) { }

    public bool IsComplete => false;

    public int CalculateRequiredHeight(int areaHeight) => _height;

    public void Render(int areaHeight)
    {
        // widget draws directly to console; fill remaining lines so
        // the pane height stays constant like the Rust version
        for (int i = 1; i < _height; i++)
            Console.WriteLine();
    }

    public ConditionalUpdate UpdateStatusText(string text)
    {
        _widget.UpdateText(text);
        return ConditionalUpdate.NeedsRedraw;
    }

    public bool ShouldHideWhenTaskIsDone() => true;

    public Event? TryConsumeApprovalRequest(Event request) => request;
}
