using CodexCli.Config;
using CodexCli.Protocol;

namespace CodexCli.Commands;

public record InteractiveOptions(
    string? Prompt,
    FileInfo[] Images,
    string? Model,
    string? Profile,
    string? ModelProvider,
    bool FullAuto,
    ApprovalMode? Approval,
    SandboxPermission[] Sandbox,
    ColorMode Color,
    bool SkipGitRepoCheck,
    string? Cwd,
    string[] NotifyCommand,
    string[] Overrides,
    ReasoningEffort? ReasoningEffort,
    ReasoningSummary? ReasoningSummary,
    string? InstructionsPath,
    bool? HideAgentReasoning,
    bool? DisableResponseStorage,
    string? LastMessageFile,
    bool NoProjectDoc,
    string? EventLogFile,
    ShellEnvironmentPolicyInherit? EnvInherit,
    bool? EnvIgnoreDefaultExcludes,
    string[] EnvExclude,
    string[] EnvSet,
    string[] EnvIncludeOnly,
    int? ProjectDocMaxBytes,
    string? ProjectDocPath);
