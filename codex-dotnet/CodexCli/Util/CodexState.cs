using CodexCli.Models;
using CodexCli.Protocol;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CodexCli.Util;

/// <summary>
/// Ported from codex-rs/core/src/codex.rs `State.partial_clone` (done).
/// Represents mutable state for a Codex session.
/// </summary>
public class CodexState
{
public HashSet<List<string>> ApprovedCommands { get; } = new(new SequenceEqualityComparer<string>());
    public string? PreviousResponseId { get; set; }
    public ConversationHistory? ZdrTranscript { get; set; }
    public bool HasCurrentTask { get; set; }
    public AgentTask? CurrentTask { get; set; }
    public List<ResponseInputItem> PendingInput { get; } = new();
    public Dictionary<string, TaskCompletionSource<ReviewDecision>> PendingApprovals { get; } = new();
    public List<string> WritableRoots { get; set; } = new();

    public CodexState PartialClone(bool retainZdrTranscript)
    {
        var clone = new CodexState
        {
            PreviousResponseId = this.PreviousResponseId,
            ZdrTranscript = retainZdrTranscript ? this.ZdrTranscript?.Clone() : null
        };
        foreach (var cmd in ApprovedCommands)
            clone.ApprovedCommands.Add(new List<string>(cmd));
        clone.HasCurrentTask = this.HasCurrentTask;
        clone.WritableRoots = new List<string>(this.WritableRoots);
        // current task is intentionally not cloned
        return clone;
    }
}
