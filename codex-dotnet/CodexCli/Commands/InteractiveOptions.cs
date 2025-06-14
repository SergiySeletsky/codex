namespace CodexCli.Commands;

public record InteractiveOptions(
    string? Prompt,
    FileInfo[] Images,
    string? Model,
    string? Profile,
    bool FullAuto,
    ApprovalMode? Approval,
    SandboxPermission[] Sandbox,
    bool SkipGitRepoCheck,
    string? Cwd);

