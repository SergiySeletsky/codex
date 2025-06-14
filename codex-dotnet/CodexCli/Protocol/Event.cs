namespace CodexCli.Protocol;

public abstract record Event(string Id);

public record AgentMessageEvent(string Id, string Message) : Event(Id);
public record ErrorEvent(string Id, string Message) : Event(Id);
public record BackgroundEvent(string Id, string Message) : Event(Id);
public record ExecCommandBeginEvent(string Id, IReadOnlyList<string> Command, string Cwd) : Event(Id);
public record ExecCommandEndEvent(string Id, string Stdout, string Stderr, int ExitCode) : Event(Id);
public record TaskCompleteEvent(string Id, string? LastAgentMessage) : Event(Id);
