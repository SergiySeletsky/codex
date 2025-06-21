using CodexCli.Util;
using System.Text.Json;

public class UserNotificationTests
{
    [Fact]
    public void SerializesAgentTurnComplete()
    {
        var notif = new AgentTurnCompleteNotification(
            "12345",
            new[]{"Rename `foo` to `bar` and update the callsites."},
            "Rename complete and verified `cargo build` succeeds."
        );
        var opts = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var json = JsonSerializer.Serialize(notif, opts);
        Assert.Equal(
            "{\"turn-id\":\"12345\",\"input-messages\":[\"Rename `foo` to `bar` and update the callsites.\"],\"last-assistant-message\":\"Rename complete and verified `cargo build` succeeds.\",\"type\":\"agent-turn-complete\"}",
            json);
    }
}
