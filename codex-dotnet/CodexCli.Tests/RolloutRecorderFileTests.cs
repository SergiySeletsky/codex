using CodexCli.Util;
using CodexCli.Models;
using CodexCli.Config;
using Xunit;

public class RolloutRecorderFileTests
{
    [Fact]
    public async Task RecordsItemsToFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var cfg = new AppConfig { CodexHome = dir };
        await using var rec = await RolloutRecorder.CreateAsync(cfg, "sess", null);
        var item = new MessageItem("assistant", new List<ContentItem>{ new("output_text", "hi") });
        await rec.RecordItemsAsync(new[]{ item });
        var file = Directory.GetFiles(Path.Combine(dir, "sessions"))[0];
        var lines = File.ReadAllLines(file);
        Assert.Contains("sess", lines[0]);
        Assert.True(lines.Length >= 2 && lines[1].Length > 2);
    }
}
