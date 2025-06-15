namespace CodexCli.Models;

public record ExecParams(List<string> Command, string Cwd, int? TimeoutMs, Dictionary<string,string> Env);

public record ExecToolCallOutput(int ExitCode, string Stdout, string Stderr, TimeSpan Duration);

public record ShellToolCallParams(List<string> Command, string? Workdir, int? TimeoutMs);
