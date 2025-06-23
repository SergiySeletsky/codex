namespace CodexCli.Protocol;

/// <summary>
/// Ported from codex-rs/core/src/exec.rs `SandboxType` (done).
/// </summary>
public enum SandboxType
{
    None,
    /// <summary>Only available on macOS.</summary>
    MacosSeatbelt,
    /// <summary>Only available on Linux.</summary>
    LinuxSeccomp,
}

