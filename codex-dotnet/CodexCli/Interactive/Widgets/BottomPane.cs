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
        _composer.SetInputFocus(focus);
    }

    public void SetTaskRunning(bool running)
    {
        _isTaskRunning = running;
    }

    public int CalculateRequiredHeight(int areaHeight)
    {
        return _composer.CalculateRequiredHeight(areaHeight);
    }

    public void Render(int areaHeight)
    {
        if (_activeView != null)
            _activeView.Render(areaHeight);
        else
            _composer.Render(areaHeight);
    }

    public bool IsCommandPopupVisible => _activeView == null && _composer.IsCommandPopupVisible;

    public void SetHistoryMetadata(string logId, int count) =>
        _composer.SetHistoryMetadata(logId, count);

    public void OnHistoryEntryResponse(string logId, int offset, string? entry)
    {
        if (_composer.OnHistoryEntryResponse(logId, offset, entry))
            RequestRedraw();
    }

    internal void RequestRedraw() { /* no-op for console prototype */ }

    public ReviewDecision PushApprovalRequest(Event req)
    {
        if (_activeView is ApprovalModalView modal)
        {
            if (modal.TryConsumeApprovalRequest(req) == null)
                return modal.Decision;
        }
        else if (_activeView != null)
        {
            var next = _activeView.TryConsumeApprovalRequest(req);
            if (next == null)
                return ReviewDecision.Denied;
            req = next;
        }

        var view = new ApprovalModalView(req);
        _activeView = view;
        RequestRedraw();
        var decision = view.Decision;
        _activeView = null;
        return decision;
    }
}
