using System;
using CodexCli.Protocol;

namespace CodexCli.Interactive;

/// <summary>
/// Simplified approval modal capturing ReviewDecision via UserApprovalWidget.
/// Mirrors codex-rs/tui/src/bottom_pane/approval_modal_view.rs (rendering done).
/// </summary>
internal class ApprovalModalView : IBottomPaneView
{
    private readonly Queue<Event> _queue = new();
    private readonly UserApprovalWidget _widget = new();
    private bool _done;
    public ReviewDecision Decision { get; private set; } = ReviewDecision.Denied;

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
                Decision = _widget.PromptExec(e.Command.ToArray(), Environment.CurrentDirectory);
                break;
            case PatchApplyApprovalRequestEvent p:
                Decision = _widget.PromptPatch(p.PatchSummary);
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
