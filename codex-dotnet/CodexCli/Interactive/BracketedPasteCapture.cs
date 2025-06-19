// Rust counterpart in codex-rs/tui/src/tui.rs (bracketed paste capture done)
using System;

namespace CodexCli.Interactive;

/// <summary>
/// Enables bracketed paste mode by writing terminal escape sequences.
/// Mirrors codex-rs/tui/src/tui.rs initialization (done).
/// </summary>
public sealed class BracketedPasteCapture : IDisposable
{
    public bool IsActive { get; private set; }

    public BracketedPasteCapture(bool active)
    {
        IsActive = active;
        Console.Write(active ? "\u001b[?2004h" : "\u001b[?2004l");
    }

    public void SetActive(bool active)
    {
        if (active == IsActive) return;
        Console.Write(active ? "\u001b[?2004h" : "\u001b[?2004l");
        IsActive = active;
    }

    public void Dispose() => SetActive(false);
}
