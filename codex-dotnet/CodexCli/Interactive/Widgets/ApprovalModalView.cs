using System;
using CodexCli.Protocol;

namespace CodexCli.Interactive;

/// <summary>
/// Placeholder modal for user approvals.
/// Mirrors codex-rs/tui/src/bottom_pane/approval_modal_view.rs (in progress).
/// </summary>
internal class ApprovalModalView : IBottomPaneView
{
    private readonly UserApprovalWidget _widget = new();
    private bool _done;

    public ApprovalModalView(Event request)
    {
        Consume(request);
    }

    private void Consume(Event request)
    {
        switch (request)
        {
            case ExecApprovalRequestEvent e:
                _widget.PromptExec(e.Command.ToArray(), Environment.CurrentDirectory);
                _done = true;
                break;
            case PatchApplyApprovalRequestEvent p:
                _widget.PromptPatch(p.PatchSummary);
                _done = true;
                break;
        }
    }

    public void HandleKeyEvent(ConsoleKeyInfo key, BottomPane pane) { }

    public bool IsComplete => _done;

    public int CalculateRequiredHeight(int areaHeight) => 1;

    public void Render(int areaHeight) { }

    public Event? TryConsumeApprovalRequest(Event request)
    {
        if (_done)
            return request;
        Consume(request);
        return null;
    }
}
