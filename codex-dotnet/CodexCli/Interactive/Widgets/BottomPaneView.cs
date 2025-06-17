using CodexCli.Protocol;
using System;

namespace CodexCli.Interactive;

/// <summary>
/// Interface mirrored from codex-rs/tui/src/bottom_pane/bottom_pane_view.rs (done).
/// </summary>
public interface IBottomPaneView
{
    void HandleKeyEvent(ConsoleKeyInfo key, BottomPane pane) { }

    bool IsComplete { get; }

    int CalculateRequiredHeight(int areaHeight);

    void Render(int areaHeight);

    ConditionalUpdate UpdateStatusText(string text) => ConditionalUpdate.NoRedraw;

    bool ShouldHideWhenTaskIsDone() => false;

    Event? TryConsumeApprovalRequest(Event request) => request;
}

public enum ConditionalUpdate
{
    NeedsRedraw,
    NoRedraw
}
