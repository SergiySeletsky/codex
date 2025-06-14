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

/// <summary>
/// Sandbox permissions controlling what the executed commands may access.
/// This is a very small subset of the Rust implementation but sufficient for
/// basic testing.
/// </summary>
public enum SandboxPermissionType
{
    DiskFullReadAccess,
    DiskWriteCwd,
    DiskWritePlatformUserTempFolder,
    DiskWritePlatformGlobalTempFolder,
    DiskWriteFolder,
    DiskFullWriteAccess,
    NetworkFullAccess,
}

public readonly record struct SandboxPermission(SandboxPermissionType Type, string? Path = null)
{
    public override string ToString()
    {
        return Type switch
        {
            SandboxPermissionType.DiskWriteFolder => $"disk-write-folder={Path}",
            _ => Type.ToString()
        };
    }
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
