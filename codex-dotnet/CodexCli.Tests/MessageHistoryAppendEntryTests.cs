using CodexCli.Util;
using CodexCli.Config;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class MessageHistoryAppendEntryTests
{
    [Fact(Skip="requires filesystem race conditions")]
    public async Task AppendConcurrentWrites()
    {
        var dir = Path.Combine(Path.GetTempPath(), "mh" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var cfg = new AppConfig { CodexHome = dir };
        var tasks = Enumerable.Range(0, 3).Select(i => MessageHistory.AppendEntryAsync($"m{i}", "s", cfg));
        await Task.WhenAll(tasks);
        var meta = await MessageHistory.HistoryMetadataAsync(cfg);
        Assert.Equal(3, meta.Count);
        Directory.Delete(dir, true);
    }

    [Fact(Skip="requires unix permissions")]
    public async Task PermissionsSetOnUnix()
    {
        if (OperatingSystem.IsWindows()) return;
        var dir = Path.Combine(Path.GetTempPath(), "mh" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var cfg = new AppConfig { CodexHome = dir };
        await MessageHistory.AppendEntryAsync("x", "s", cfg);
        var file = MessageHistory.GetHistoryFile(cfg);
        var info = new Mono.Unix.UnixFileInfo(file);
        Assert.Equal(Mono.Unix.FileAccessPermissions.UserRead | Mono.Unix.FileAccessPermissions.UserWrite, info.FileAccessPermissions & (Mono.Unix.FileAccessPermissions)0x1FF);
        Directory.Delete(dir, true);
    }
}
