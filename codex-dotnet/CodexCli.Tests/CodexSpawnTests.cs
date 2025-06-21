using CodexCli.Protocol;
using CodexCli.Util;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class CodexSpawnTests
{
    [Fact]
    public async Task SpawnReturnsEvents()
    {
        var (codex, initId) = await Codex.SpawnAsync(
            "hi",
            new OpenAIClient(null, "http://localhost"),
            "gpt-4",
            (p,c,m,t) => MockCodexAgent.RunAsync(p, new string[0], null, t));

        Assert.False(string.IsNullOrEmpty(initId));
        var list = new List<Event>();
        Event? ev;
        while ((ev = await codex.NextEventAsync()) != null)
            list.Add(ev);
        Assert.Contains(list, e => e is TaskCompleteEvent);
    }
}
