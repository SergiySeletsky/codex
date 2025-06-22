namespace CodexCli.Util;

/// <summary>
/// Ported from codex-rs/core/src/codex.rs `State.partial_clone` (done).
/// Represents mutable state for a Codex session.
/// </summary>
public class CodexState
{
    public HashSet<List<string>> ApprovedCommands { get; } = new();
    public string? PreviousResponseId { get; set; }
    public ConversationHistory? ZdrTranscript { get; set; }

    public CodexState PartialClone(bool retainZdrTranscript)
    {
        var clone = new CodexState
        {
            PreviousResponseId = this.PreviousResponseId,
            ZdrTranscript = retainZdrTranscript ? this.ZdrTranscript?.Clone() : null
        };
        foreach (var cmd in ApprovedCommands)
            clone.ApprovedCommands.Add(new List<string>(cmd));
        return clone;
    }
}
