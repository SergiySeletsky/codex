using CodexCli.Protocol;
using CodexCli.Util;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class CodexWrapperTests
{
    [Fact]
    public async Task ExtractsSessionConfigured()
    {
        var (stream, first, _cts) = await CodexWrapper.InitCodexAsync(
            "hi",
            new OpenAIClient(null, "http://localhost"),
            "gpt-4",
            (p, c, m, t) => MockCodexAgent.RunAsync(p, new string[0], null, t));
        Assert.IsType<SessionConfiguredEvent>(first);
        List<Event> list = new();
        await foreach (var ev in stream)
            list.Add(ev);
        Assert.Contains(list, e => e is TaskCompleteEvent);
    }
}
