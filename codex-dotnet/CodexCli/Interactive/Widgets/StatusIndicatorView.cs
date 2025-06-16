using CodexCli.Protocol;

namespace CodexCli.Interactive;

/// <summary>
/// Wrapper view for StatusIndicatorWidget.
/// Mirrors codex-rs/tui/src/bottom_pane/status_indicator_view.rs (done).
/// </summary>
internal class StatusIndicatorView : IBottomPaneView
{
    private readonly StatusIndicatorWidget _widget;

    public StatusIndicatorView(StatusIndicatorWidget widget)
    {
        _widget = widget;
    }

    public void HandleKeyEvent(ConsoleKeyInfo key, BottomPane pane) { }

    public bool IsComplete => false;

    public int CalculateRequiredHeight(int areaHeight) => 1;

    public void Render(int areaHeight)
    {
        // widget draws directly to console
    }

    public ConditionalUpdate UpdateStatusText(string text)
    {
        _widget.UpdateText(text);
        return ConditionalUpdate.NeedsRedraw;
    }

    public bool ShouldHideWhenTaskIsDone() => true;

    public Event? TryConsumeApprovalRequest(Event request) => request;
}
