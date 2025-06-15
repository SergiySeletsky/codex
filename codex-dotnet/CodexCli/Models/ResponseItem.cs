namespace CodexCli.Models;

using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MessageItem), typeDiscriminator: "message")]
[JsonDerivedType(typeof(FunctionCallItem), typeDiscriminator: "function_call")]
[JsonDerivedType(typeof(FunctionCallOutputItem), typeDiscriminator: "function_call_output")]
[JsonDerivedType(typeof(LocalShellCallItem), typeDiscriminator: "local_shell_call")]
[JsonDerivedType(typeof(ReasoningItem), typeDiscriminator: "reasoning")]
[JsonDerivedType(typeof(OtherItem), typeDiscriminator: "other")]
public abstract record ResponseItem;

public static class ResponseItemFactory
{
    public static ResponseItem? FromEvent(CodexCli.Protocol.Event ev)
        => ev switch
        {
            CodexCli.Protocol.AgentMessageEvent am => new MessageItem("assistant", new List<ContentItem>{ new("output_text", am.Message) }),
            CodexCli.Protocol.AddToHistoryEvent ah => new MessageItem("user", new List<ContentItem>{ new("output_text", ah.Text) }),
            _ => null
        };
}

public record ContentItem(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text
);

public record MessageItem(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] List<ContentItem> Content
) : ResponseItem;

public record FunctionCallItem(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("arguments")] string Arguments,
    [property: JsonPropertyName("call_id")] string CallId
) : ResponseItem;

public record FunctionCallOutputPayload(
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("success")] bool? Success
);
public record FunctionCallOutputItem(
    [property: JsonPropertyName("call_id")] string CallId,
    [property: JsonPropertyName("output")] FunctionCallOutputPayload Output
) : ResponseItem;

public enum LocalShellStatus { Completed, InProgress, Incomplete }

public record LocalShellExecAction(
    [property: JsonPropertyName("command")] List<string> Command,
    [property: JsonPropertyName("timeout_ms")] int? TimeoutMs,
    [property: JsonPropertyName("working_directory")] string? WorkingDirectory
);
public record LocalShellAction([property: JsonPropertyName("exec")] LocalShellExecAction Exec);
public record LocalShellCallItem(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("call_id")] string? CallId,
    [property: JsonPropertyName("status")] LocalShellStatus Status,
    [property: JsonPropertyName("action")] LocalShellAction Action
) : ResponseItem;

public record ReasoningItemReasoningSummary([property: JsonPropertyName("text")] string Text);
public record ReasoningItem(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("summary")] List<ReasoningItemReasoningSummary> Summary
) : ResponseItem;

public record OtherItem() : ResponseItem;

public abstract record ResponseInputItem;
public record MessageInputItem(string Role, List<ContentItem> Content) : ResponseInputItem;
public record FunctionCallOutputInputItem(string CallId, FunctionCallOutputPayload Output) : ResponseInputItem;
public record McpToolCallOutputInputItem(string CallId, string ResultJson) : ResponseInputItem;
