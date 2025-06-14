namespace CodexCli.Protocol;

public abstract record Event(string Id);

public record AgentMessageEvent(string Id, string Message) : Event(Id);
public record ErrorEvent(string Id, string Message) : Event(Id);
public record BackgroundEvent(string Id, string Message) : Event(Id);
public record ExecCommandBeginEvent(string Id, IReadOnlyList<string> Command, string Cwd) : Event(Id);
public record ExecCommandEndEvent(string Id, string Stdout, string Stderr, int ExitCode) : Event(Id);
public record TaskCompleteEvent(string Id, string? LastAgentMessage) : Event(Id);
public record ExecApprovalRequestEvent(string Id, IReadOnlyList<string> Command) : Event(Id);
public record PatchApplyApprovalRequestEvent(string Id, string PatchSummary) : Event(Id);
public record PatchApplyBeginEvent(string Id, bool AutoApproved, IReadOnlyDictionary<string,string> Changes) : Event(Id);
public record PatchApplyEndEvent(string Id, string Stdout, string Stderr, bool Success) : Event(Id);
public record McpToolCallBeginEvent(string Id, string Server, string Tool, string? ArgumentsJson) : Event(Id);
public record McpToolCallEndEvent(string Id, bool IsSuccess, string ResultJson) : Event(Id);
public record AgentReasoningEvent(string Id, string Text) : Event(Id);
public record SessionConfiguredEvent(string Id, string SessionId, string Model) : Event(Id);
