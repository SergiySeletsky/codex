using CodexCli.Config;
using CodexCli.Protocol;

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
    string? SessionId,
    bool SkipGitRepoCheck,
    string[] NotifyCommand,
    string[] Overrides,
    ReasoningEffort? ReasoningEffort,
    ReasoningSummary? ReasoningSummary,
    string? InstructionsPath,
    bool? HideAgentReasoning,
    bool? DisableResponseStorage,
    bool NoProjectDoc,
    bool Json,
    string? EventLogFile,
    ShellEnvironmentPolicyInherit? EnvInherit,
    bool? EnvIgnoreDefaultExcludes,
    string[] EnvExclude,
    string[] EnvSet,
    string[] EnvIncludeOnly,
    int? ProjectDocMaxBytes,
    string? ProjectDocPath,
    string? McpServer);
