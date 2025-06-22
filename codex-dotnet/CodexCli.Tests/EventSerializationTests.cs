using CodexCli.Protocol;
using System.Text.Json;
using Xunit;

public class EventSerializationTests
{
    [Fact]
    public void SessionConfiguredSerializes()
    {
        Event ev = new SessionConfiguredEvent("1234","67e55044-10b1-426f-9247-bb680e5fe0c8","codex-mini-latest");
        var json = JsonSerializer.Serialize(ev);
        Assert.Equal("{\"type\":\"session_configured\",\"SessionId\":\"67e55044-10b1-426f-9247-bb680e5fe0c8\",\"Model\":\"codex-mini-latest\",\"Id\":\"1234\"}", json);
    }
}
