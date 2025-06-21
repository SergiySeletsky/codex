// Ported from codex-rs/core/src/user_notification.rs (done)
using System.Text.Json.Serialization;

namespace CodexCli.Util;

public abstract record UserNotification;

public record AgentTurnCompleteNotification(
    [property: JsonPropertyName("turn-id")] string TurnId,
    [property: JsonPropertyName("input-messages")] IReadOnlyList<string> InputMessages,
    [property: JsonPropertyName("last-assistant-message")] string? LastAssistantMessage
) : UserNotification
{
    [JsonPropertyName("type")]
    public string Type => "agent-turn-complete";
}
