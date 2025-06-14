using CodexCli.Util;
using CodexCli.Config;
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
        Directory.Delete(dir, true);
    }
}
