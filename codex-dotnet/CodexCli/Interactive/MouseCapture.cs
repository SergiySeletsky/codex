using System;

namespace CodexCli.Interactive;

/// <summary>
/// Manages terminal mouse capture sequences.
/// Mirrors codex-rs/tui/src/mouse_capture.rs (done).
/// </summary>
public sealed class MouseCapture : IDisposable
{
    public bool IsActive { get; private set; }

    public MouseCapture(bool active)
    {
        IsActive = active;
        Console.Write(active ? "\u001b[?1000h" : "\u001b[?1000l");
    }

    public void SetActive(bool active)
    {
        if (active == IsActive) return;
        Console.Write(active ? "\u001b[?1000h" : "\u001b[?1000l");
        IsActive = active;
    }

    public void Toggle() => SetActive(!IsActive);

    public void Disable() => SetActive(false);

    public void Dispose() => Disable();
}
