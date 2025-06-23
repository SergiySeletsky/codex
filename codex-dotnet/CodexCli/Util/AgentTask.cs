using System;

namespace CodexCli.Util;

/// <summary>
/// Simple representation of a running agent task used by Codex.SetTask.
/// Ported from codex-rs/core/src/codex.rs `AgentTask` (partial).
/// </summary>
public class AgentTask
{
    public string SubId { get; }
    private readonly Action _abort;
    public bool Aborted { get; private set; }

    public AgentTask(string subId, Action abort)
    {
        SubId = subId;
        _abort = abort;
    }

    public void Abort()
    {
        Aborted = true;
        _abort?.Invoke();
    }
}
