using System;
using CodexCli.Protocol;

namespace CodexCli.Interactive;

/// <summary>
/// Placeholder modal for user approvals.
/// Mirrors codex-rs/tui/src/bottom_pane/approval_modal_view.rs (in progress).
/// </summary>
internal class ApprovalModalView : IBottomPaneView
{
    private readonly Queue<Event> _queue = new();
    private readonly UserApprovalWidget _widget = new();
    private bool _done;

    public ApprovalModalView(Event request)
    {
        _queue.Enqueue(request);
        ProcessNext();
    }

    private void ProcessNext()
    {
        if (_done)
            return;
        if (!_queue.TryDequeue(out var request))
            return;
        switch (request)
        {
            case ExecApprovalRequestEvent e:
                _widget.PromptExec(e.Command.ToArray(), Environment.CurrentDirectory);
                break;
            case PatchApplyApprovalRequestEvent p:
                _widget.PromptPatch(p.PatchSummary);
                break;
        }
        _done = true;
    }

    public void HandleKeyEvent(ConsoleKeyInfo key, BottomPane pane) { }

    public bool IsComplete => _done;

    public int CalculateRequiredHeight(int areaHeight) => 1;

    public void Render(int areaHeight) { }

    public Event? TryConsumeApprovalRequest(Event request)
    {
        if (_done)
            return request;
        _queue.Enqueue(request);
        ProcessNext();
        return null;
    }
}
