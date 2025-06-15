
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CodexCli.Protocol;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AgentMessageEvent), "agent_message")]
[JsonDerivedType(typeof(ErrorEvent), "error")]
[JsonDerivedType(typeof(BackgroundEvent), "background_event")]
[JsonDerivedType(typeof(ExecCommandBeginEvent), "exec_command_begin")]
[JsonDerivedType(typeof(ExecCommandEndEvent), "exec_command_end")]
[JsonDerivedType(typeof(TaskStartedEvent), "task_started")]
[JsonDerivedType(typeof(TaskCompleteEvent), "task_complete")]
[JsonDerivedType(typeof(ExecApprovalRequestEvent), "exec_approval_request")]
[JsonDerivedType(typeof(PatchApplyApprovalRequestEvent), "apply_patch_approval_request")]
[JsonDerivedType(typeof(PatchApplyBeginEvent), "patch_apply_begin")]
[JsonDerivedType(typeof(PatchApplyEndEvent), "patch_apply_end")]
[JsonDerivedType(typeof(McpToolCallBeginEvent), "mcp_tool_call_begin")]
[JsonDerivedType(typeof(McpToolCallEndEvent), "mcp_tool_call_end")]
[JsonDerivedType(typeof(AgentReasoningEvent), "agent_reasoning")]
[JsonDerivedType(typeof(SessionConfiguredEvent), "session_configured")]
[JsonDerivedType(typeof(AddToHistoryEvent), "add_to_history")]
[JsonDerivedType(typeof(GetHistoryEntryRequestEvent), "get_history_entry_request")]
[JsonDerivedType(typeof(GetHistoryEntryResponseEvent), "get_history_entry_response")]
[JsonDerivedType(typeof(ResourceUpdatedEvent), "resource_updated")]
[JsonDerivedType(typeof(ResourceListChangedEvent), "resource_list_changed")]
[JsonDerivedType(typeof(PromptListChangedEvent), "prompt_list_changed")]
[JsonDerivedType(typeof(ToolListChangedEvent), "tool_list_changed")]
[JsonDerivedType(typeof(LoggingMessageEvent), "logging_message")]
public abstract record Event(string Id);

public record AgentMessageEvent(string Id, string Message) : Event(Id);
public record ErrorEvent(string Id, string Message) : Event(Id);
public record BackgroundEvent(string Id, string Message) : Event(Id);
public record ExecCommandBeginEvent(string Id, IReadOnlyList<string> Command, string Cwd) : Event(Id);
public record ExecCommandEndEvent(string Id, string Stdout, string Stderr, int ExitCode) : Event(Id);
public record TaskStartedEvent(string Id) : Event(Id);
public record TaskCompleteEvent(string Id, string? LastAgentMessage) : Event(Id);
public record ExecApprovalRequestEvent(string Id, IReadOnlyList<string> Command) : Event(Id);
public record PatchApplyApprovalRequestEvent(string Id, string PatchSummary) : Event(Id);

public abstract record FileChange;
public record AddFileChange(string Content) : FileChange;
public record DeleteFileChange() : FileChange;
public record UpdateFileChange(string UnifiedDiff, string? MovePath) : FileChange;

public record PatchApplyBeginEvent(string Id, bool AutoApproved, IReadOnlyDictionary<string,FileChange> Changes) : Event(Id);
public record PatchApplyEndEvent(string Id, string Stdout, string Stderr, bool Success) : Event(Id);
public record McpToolCallBeginEvent(string Id, string Server, string Tool, string? ArgumentsJson) : Event(Id);
public record McpToolCallEndEvent(string Id, bool IsSuccess, string ResultJson) : Event(Id);
public record AgentReasoningEvent(string Id, string Text) : Event(Id);
public record SessionConfiguredEvent(string Id, string SessionId, string Model) : Event(Id);
public record AddToHistoryEvent(string Id, string Text) : Event(Id);
public record GetHistoryEntryRequestEvent(string Id, string SessionId, int Offset) : Event(Id);
public record GetHistoryEntryResponseEvent(string Id, string SessionId, int Offset, string? Entry) : Event(Id);


public record ResourceUpdatedEvent(string Id, string Uri) : Event(Id);
public record ResourceListChangedEvent(string Id) : Event(Id);
public record PromptListChangedEvent(string Id) : Event(Id);
public record ToolListChangedEvent(string Id) : Event(Id);
public record LoggingMessageEvent(string Id, string Message) : Event(Id);
