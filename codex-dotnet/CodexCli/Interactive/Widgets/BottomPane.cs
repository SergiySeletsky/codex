using System;
using CodexCli.Protocol;

namespace CodexCli.Interactive;

/// <summary>
/// Container for chat composer and overlay views.
/// Mirrors codex-rs/tui/src/bottom_pane/mod.rs (status and approval overlays
/// done, interactive image command and JPEG support implemented).
/// </summary>
public class BottomPane
{
    private readonly ChatComposer _composer;
    private IBottomPaneView? _activeView;
    private readonly AppEventSender _appEventTx;
    private bool _hasInputFocus;
    private bool _isTaskRunning;

    public BottomPane(AppEventSender sender, bool hasInputFocus, ConversationHistoryWidget? history = null)
    {
        _composer = new ChatComposer(hasInputFocus, sender, history);
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
                if (_activeView is IDisposable d) d.Dispose();
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

        if (running && _activeView == null)
        {
            var widget = new StatusIndicatorWidget();
            widget.Start();
            int h = _composer.CalculateRequiredHeight(Console.WindowHeight / 2);
            _activeView = new StatusIndicatorView(widget, h);
            RequestRedraw();
        }
        else if (!running && _activeView != null)
        {
            if (_activeView.ShouldHideWhenTaskIsDone())
            {
                if (_activeView is IDisposable d) d.Dispose();
                _activeView = null;
                RequestRedraw();
            }
        }
    }

    public int CalculateRequiredHeight(int areaHeight)
    {
        return _activeView != null
            ? _activeView.CalculateRequiredHeight(areaHeight)
            : _composer.CalculateRequiredHeight(areaHeight);
    }

    public void Render(int areaHeight)
    {
        if (_activeView != null)
        {
            _activeView.Render(areaHeight);
            if (_activeView.IsComplete)
            {
                if (_activeView is IDisposable d) d.Dispose();
                _activeView = null;
            }
        }
        else
        {
            _composer.Render(areaHeight);
        }
    }

    public bool IsCommandPopupVisible => _activeView == null && _composer.IsCommandPopupVisible;

    public bool HasActiveView => _activeView != null;

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

        if (_activeView is IDisposable disp)
            disp.Dispose();
        var view = new ApprovalModalView(req);
        _activeView = view;
        RequestRedraw();
        return view.Decision;
    }
}
