using CodexCli.Util;
using CodexCli.Config;
using Xunit;

public class MessageHistoryMetadataTests
{
    [Fact]
    public async Task MetadataIncludesFileIdAndCount()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mh" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var cfg = new AppConfig { CodexHome = dir };
        await MessageHistory.AppendEntryAsync("one", "s", cfg);
        await MessageHistory.AppendEntryAsync("two", "s", cfg);

        var meta = await MessageHistory.HistoryMetadataAsync(cfg);
        Assert.Equal(2, meta.Count);
        if (!OperatingSystem.IsWindows())
            Assert.True(meta.LogId != 0UL);

        var line = MessageHistory.LookupEntry(meta.LogId, 1, cfg);
        Assert.Equal("two", line);
        Directory.Delete(dir, true);
    }
}
