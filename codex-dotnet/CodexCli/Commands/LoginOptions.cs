namespace CodexCli.Commands;

using CodexCli.Config;

public record LoginOptions(
    string[] Overrides,
    string? Token,
    string? ApiKey,
    string? Provider,
    bool ChatGpt,
    ShellEnvironmentPolicyInherit? EnvInherit,
    bool? EnvIgnoreDefaultExcludes,
    string[] EnvExclude,
    string[] EnvSet,
    string[] EnvIncludeOnly);
