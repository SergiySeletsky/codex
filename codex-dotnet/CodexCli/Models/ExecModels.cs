using System.Text.Json.Serialization;

namespace CodexCli.Models;

public record ExecParams(
    List<string> Command,
    string Cwd,
    int? TimeoutMs,
    Dictionary<string,string> Env,
    int? MaxOutputBytes = null,
    int? MaxOutputLines = null,
    string? SessionId = null);

public record ExecToolCallOutput(int ExitCode, string Stdout, string Stderr, TimeSpan Duration);

public record ShellToolCallParams(
    [property: JsonPropertyName("command")] List<string> Command,
    [property: JsonPropertyName("workdir")] string? Workdir,
    [property: JsonPropertyName("timeout_ms")] int? TimeoutMs);
