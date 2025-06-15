using CodexCli.Models;
using CodexCli.Util;
using Xunit;

public class RolloutReplayerTests
{
    [Fact]
    public async Task ReplayReturnsItems()
    {
        var tmp = Path.GetTempFileName();
        await File.WriteAllTextAsync(tmp, "line\n");
        var list = new List<string>();
        await foreach(var line in RolloutReplayer.ReplayLinesAsync(tmp))
            list.Add(line);
        Assert.Single(list);
        Assert.Equal("line", list[0]);
    }
}
