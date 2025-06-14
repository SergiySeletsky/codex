namespace CodexCli.Commands;

public record ExecOptions(
    string? Prompt,
    FileInfo[] Images,
    string? Model,
    string? Profile,
    bool FullAuto,
    string Color,
    string? LastMessageFile,
    bool SkipGitRepoCheck,
    string[] Overrides);
