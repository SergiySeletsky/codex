using CodexCli.Config;
using CodexCli.Util;

public class MessageHistoryWatchTests
{
    [Fact]
    public async Task WatchYieldsNewEntries()
    {
        var cfg = new AppConfig();
        cfg.CodexHome = Path.GetTempPath();
        MessageHistory.ClearHistory(cfg);
        var cts = new CancellationTokenSource();
        var enumerator = MessageHistory.WatchEntriesAsync(cfg, cts.Token).GetAsyncEnumerator();
        await MessageHistory.AppendEntryAsync("one", "s1", cfg);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal("one", enumerator.Current);
        cts.Cancel();
        await enumerator.DisposeAsync();
    }
}
