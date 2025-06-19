namespace CodexCli.Interactive;

/// <summary>
/// Simplified logger bridge forwarding latest log lines to the UI.
/// Mirrors codex-rs/tui/src/lib.rs log forwarding (done).
/// </summary>
public static class LogBridge
{
    public static event Action<string>? LatestLog;

    public static void Emit(string line)
    {
        try { LatestLog?.Invoke(line); } catch { }
    }
}
