using System.Text.Json.Serialization;

namespace CodexCli.Util;

[JsonDerivedType(typeof(AgentTurnCompleteNotification), typeDiscriminator:"agent-turn-complete")]
public abstract record UserNotification;

public record AgentTurnCompleteNotification(
    [property: JsonPropertyName("turn-id")] string TurnId,
    [property: JsonPropertyName("input-messages")] IReadOnlyList<string> InputMessages,
    [property: JsonPropertyName("last-assistant-message")] string? LastAssistantMessage
) : UserNotification;
