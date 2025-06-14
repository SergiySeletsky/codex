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
public enum SandboxPermission
{
    DiskFullReadAccess,
    DiskWriteCwd,
    DiskWritePlatformTemp,
    DiskFullWriteAccess,
    NetworkFullAccess,
}

