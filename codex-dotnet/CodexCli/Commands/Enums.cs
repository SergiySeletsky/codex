namespace CodexCli.Commands;

/// <summary>
/// Policy for when the agent should ask the user to approve execution.
/// </summary>
public enum ApprovalMode
{
    OnFailure,
    UnlessAllowListed,
    Never,
}



public enum ReasoningEffort
{
    None,
    Low,
    Medium,
    High
}

public enum ReasoningSummary
{
    None,
    Brief,
    Detailed
}
