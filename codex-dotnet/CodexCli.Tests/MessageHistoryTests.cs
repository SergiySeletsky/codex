using CodexCli.Util;
using CodexCli.Config;
using System.Linq;
using Xunit;

public class MessageHistoryTests
{
    [Fact]
    public async Task AppendAndLookup()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mh" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var cfg = new AppConfig { CodexHome = dir };
        await MessageHistory.AppendEntryAsync("hello", "s1", cfg);
        await MessageHistory.AppendEntryAsync("world", "s1", cfg);
        var meta = await MessageHistory.HistoryMetadataAsync(cfg);
        Assert.Equal(2, meta.Count);
        var e1 = MessageHistory.LookupEntry(meta.LogId, 1, cfg);
        Assert.Equal("world", e1);
        var count = await MessageHistory.CountEntriesAsync(cfg);
        Assert.Equal(2, count);
        var last = await MessageHistory.LastEntriesAsync(1, cfg);
        Assert.Single(last);
        Assert.Equal("world", last.First());
        var search = await MessageHistory.SearchEntriesAsync("hello", cfg);
        Assert.Single(search);
        MessageHistory.ClearHistory(cfg);
        var meta2 = await MessageHistory.HistoryMetadataAsync(cfg);
        Assert.Equal(0, meta2.Count);
        Directory.Delete(dir, true);
    }

    [Fact]
    public async Task SessionStats()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mh" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var cfg = new AppConfig { CodexHome = dir };
        await MessageHistory.AppendEntryAsync("a", "s1", cfg);
        await MessageHistory.AppendEntryAsync("b", "s2", cfg);
        await MessageHistory.AppendEntryAsync("c", "s1", cfg);
        var stats = await MessageHistory.SessionStatsAsync(cfg);
        Assert.Equal(2, stats["s1"]);
        Assert.Equal(1, stats["s2"]);
        Directory.Delete(dir, true);
    }
}
