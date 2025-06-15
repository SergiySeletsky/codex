namespace CodexCli.Models;

using System.Text.Json.Serialization;
using System.Linq;
using System.Text.Json;

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
            CodexCli.Protocol.AgentReasoningEvent ar => new ReasoningItem(Guid.NewGuid().ToString(), new List<ReasoningItemReasoningSummary>{ new(ar.Text) }),
            CodexCli.Protocol.BackgroundEvent be => new MessageItem("system", new List<ContentItem>{ new("output_text", be.Message) }),
            CodexCli.Protocol.ErrorEvent ee => new MessageItem("system", new List<ContentItem>{ new("output_text", ee.Message) }),
            CodexCli.Protocol.ExecCommandBeginEvent eb => new LocalShellCallItem(null, eb.Id, LocalShellStatus.InProgress, new LocalShellAction(new LocalShellExecAction(eb.Command.ToList(), null, eb.Cwd))),
            CodexCli.Protocol.ExecCommandEndEvent ec => new LocalShellCallItem(null, ec.Id, LocalShellStatus.Completed, new LocalShellAction(new LocalShellExecAction(new List<string>(), null, string.Empty))),
            CodexCli.Protocol.McpToolCallBeginEvent mb => new FunctionCallItem(mb.Tool, mb.ArgumentsJson ?? string.Empty, mb.Id),
            CodexCli.Protocol.McpToolCallEndEvent me => new FunctionCallOutputItem(me.Id, new FunctionCallOutputPayload(me.ResultJson, me.IsSuccess)),
            CodexCli.Protocol.TaskStartedEvent ts => new OtherItem(),
            CodexCli.Protocol.TaskCompleteEvent tc => tc.LastAgentMessage != null ?
                new MessageItem("assistant", new List<ContentItem>{ new("output_text", tc.LastAgentMessage) }) : new OtherItem(),
            CodexCli.Protocol.ExecApprovalRequestEvent ea => new MessageItem("system", new List<ContentItem>{ new("output_text", $"Approve exec: {string.Join(' ', ea.Command)}") }),
            CodexCli.Protocol.PatchApplyApprovalRequestEvent pa => new MessageItem("system", new List<ContentItem>{ new("output_text", $"Approve patch: {pa.PatchSummary}") }),
            CodexCli.Protocol.PatchApplyBeginEvent pb => new MessageItem("system", new List<ContentItem>{ new("output_text", "Applying patch") }),
            CodexCli.Protocol.PatchApplyEndEvent pe => new MessageItem("system", new List<ContentItem>{ new("output_text", pe.Success ? "Patch applied" : "Patch failed") }),
            CodexCli.Protocol.ResourceUpdatedEvent ru => new MessageItem("system", new List<ContentItem>{ new("output_text", $"Resource updated {ru.Uri}") }),
            CodexCli.Protocol.ResourceListChangedEvent => new OtherItem(),
            CodexCli.Protocol.PromptListChangedEvent => new OtherItem(),
            CodexCli.Protocol.ToolListChangedEvent => new OtherItem(),
            CodexCli.Protocol.LoggingMessageEvent lm => new MessageItem("system", new List<ContentItem>{ new("output_text", lm.Message) }),
            CodexCli.Protocol.CancelledNotificationEvent cn => new MessageItem("system", new List<ContentItem>{ new("output_text", $"Request {cn.RequestId} cancelled: {cn.Reason}") }),
            CodexCli.Protocol.ProgressNotificationEvent pn => new MessageItem("system", new List<ContentItem>{ new("output_text", $"Progress {pn.Progress}") }),
            _ => null
        };

    public static ResponseItem? FromJson(string json)
    {
        ResponseItem? item = null;
        try { item = JsonSerializer.Deserialize<ResponseItem>(json); }
        catch { }
        if (item != null) return item;
        try
        {
            var ev = JsonSerializer.Deserialize<CodexCli.Protocol.Event>(json);
            if (ev != null) return FromEvent(ev);
        }
        catch { }
        return null;
    }
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
