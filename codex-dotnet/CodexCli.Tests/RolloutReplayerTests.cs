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

    [Fact]
    public async Task ReplayParsesItems()
    {
        var tmp = Path.GetTempFileName();
        var itemJson = System.Text.Json.JsonSerializer.Serialize(new MessageItem("assistant", new List<ContentItem>{ new("output_text","hi") }));
        await File.WriteAllTextAsync(tmp, itemJson + "\n");
        await foreach(var item in RolloutReplayer.ReplayAsync(tmp))
        {
            Assert.IsType<MessageItem>(item);
            var msg = (MessageItem)item;
            Assert.Equal("assistant", msg.Role);
        }
    }
}
