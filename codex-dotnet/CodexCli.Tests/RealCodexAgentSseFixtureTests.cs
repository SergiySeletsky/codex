using CodexCli.Protocol;
using CodexCli.Models;
using CodexCli.Util;
using CodexCli.Config;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

public class RealCodexAgentSseFixtureTests
{
    [Fact]
    public async Task StreamsFixture()
    {
        var content = "event: response.output_item.done\n" +
                      "data: {\"type\":\"response.output_item.done\",\"item\":{\"type\":\"message\",\"role\":\"assistant\",\"content\":[{\"type\":\"text\",\"text\":\"hi\"}]}}\n\n" +
                      "event: response.completed\n" +
                      "data: {\"type\":\"response.completed\",\"response\":{\"id\":\"r1\",\"output\":[]}}\n\n";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        Environment.SetEnvironmentVariable("CODEX_RS_SSE_FIXTURE", path);
        var events = new List<Event>();
        await foreach (var ev in RealCodexAgent.RunAsync("hi", new OpenAIClient(null, "http://localhost"), "gpt-4"))
            events.Add(ev);
        Environment.SetEnvironmentVariable("CODEX_RS_SSE_FIXTURE", null);
        File.Delete(path);
        Assert.Contains(events, e => e is AgentMessageEvent);
        Assert.Contains(events, e => e is TaskCompleteEvent);
    }
}
