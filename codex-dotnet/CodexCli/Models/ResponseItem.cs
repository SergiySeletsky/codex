namespace CodexCli.Models;

using System.Text.Json.Serialization;

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

public record ContentItem([property: JsonPropertyName("type")] string Type, string Text);

public record MessageItem(string Role, List<ContentItem> Content) : ResponseItem;

public record FunctionCallItem(string Name, string Arguments, string CallId) : ResponseItem;

public record FunctionCallOutputPayload(string Content, bool? Success);
public record FunctionCallOutputItem(string CallId, FunctionCallOutputPayload Output) : ResponseItem;

public enum LocalShellStatus { Completed, InProgress, Incomplete }

public record LocalShellExecAction(List<string> Command, int? TimeoutMs, string? WorkingDirectory);
public record LocalShellAction(LocalShellExecAction Exec);
public record LocalShellCallItem(string? Id, string? CallId, LocalShellStatus Status, LocalShellAction Action) : ResponseItem;

public record ReasoningItemReasoningSummary(string Text);
public record ReasoningItem(string Id, List<ReasoningItemReasoningSummary> Summary) : ResponseItem;

public record OtherItem() : ResponseItem;

public abstract record ResponseInputItem;
public record MessageInputItem(string Role, List<ContentItem> Content) : ResponseInputItem;
public record FunctionCallOutputInputItem(string CallId, FunctionCallOutputPayload Output) : ResponseInputItem;
public record McpToolCallOutputInputItem(string CallId, string ResultJson) : ResponseInputItem;
