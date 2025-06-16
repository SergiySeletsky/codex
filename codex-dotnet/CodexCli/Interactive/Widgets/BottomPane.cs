using System;
using CodexCli.Protocol;

namespace CodexCli.Interactive;

/// <summary>
/// Container for chat composer and overlay views.
/// Mirrors codex-rs/tui/src/bottom_pane/mod.rs (in progress).
/// </summary>
public class BottomPane
{
    private readonly ChatComposer _composer;
    private IBottomPaneView? _activeView;
    private readonly AppEventSender _appEventTx;
    private bool _hasInputFocus;
    private bool _isTaskRunning;

    public BottomPane(AppEventSender sender, bool hasInputFocus)
    {
        _composer = new ChatComposer(hasInputFocus, sender);
        _appEventTx = sender;
        _hasInputFocus = hasInputFocus;
    }

    public InputResult HandleKeyEvent(ConsoleKeyInfo key)
    {
        if (_activeView != null)
        {
            _activeView.HandleKeyEvent(key, this);
            if (_activeView.IsComplete)
            {
                _activeView = null;
            }
            return InputResult.None;
        }
        var (res, redraw) = _composer.HandleKeyEvent(key);
        if (redraw) RequestRedraw();
        return res;
    }

    public void UpdateStatusText(string text)
    {
        if (_activeView != null)
        {
            if (_activeView.UpdateStatusText(text) == ConditionalUpdate.NeedsRedraw)
                RequestRedraw();
        }
    }

    public void SetInputFocus(bool focus)
    {
        _hasInputFocus = focus;
    }

    public void SetTaskRunning(bool running)
    {
        _isTaskRunning = running;
    }

    public int CalculateRequiredHeight(int areaHeight)
    {
        return 1;
    }

    internal void RequestRedraw() { }

    public void PushApprovalRequest(Event req) { }
}
