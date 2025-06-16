using System;
using CodexCli.Protocol;

namespace CodexCli.Interactive;

/// <summary>
/// Lightweight event dispatcher used by TUI components.
/// Mirrors codex-rs/tui/src/app_event_sender.rs (done).
/// </summary>
public class AppEventSender
{
    private readonly Action<Event> _send;

    public AppEventSender(Action<Event> send)
    {
        _send = send;
    }

    public void Send(Event ev)
    {
        try { _send(ev); } catch { }
    }
}
