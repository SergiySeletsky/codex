using CodexCli.Protocol;
using CodexCli.Util;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

public class CodexToolRunnerTests
{
    [Fact]
    public async Task EmitsSessionConfigured()
    {
        List<Event> events = new();
        var param = new CodexToolCallParam("hi", Provider: "mock");
        var result = await CodexToolRunner.RunCodexToolSessionAsync(param, e => events.Add(e));
        Assert.NotEmpty(events);
        Assert.IsType<SessionConfiguredEvent>(events[0]);
        Assert.Equal("codex done", result.Content[0].GetString());
    }
}

