namespace CodexCli.Commands;

public enum ColorMode
{
    Always,
    Never,
    Auto
}

public record ExecOptions(
    string? Prompt,
    FileInfo[] Images,
    string? Model,
    string? Profile,
    string? ModelProvider,
    bool FullAuto,
    ApprovalMode? Approval,
    SandboxPermission[] Sandbox,
    ColorMode Color,
    string? Cwd,
    string? LastMessageFile,
    bool SkipGitRepoCheck,
    string[] NotifyCommand,
    string[] Overrides,
    ReasoningEffort? ReasoningEffort,
    ReasoningSummary? ReasoningSummary,
    string? InstructionsPath);
