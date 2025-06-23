using CodexCli.Config;
using CodexCli.Models;
using CodexCli.Util;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public class CodexRecordRolloutItemsTests
{
    [Fact]
    public async Task WritesToFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var cfg = new AppConfig { CodexHome = dir };
        await using var rec = await RolloutRecorder.CreateAsync(cfg, "sess", null);
        var item = new MessageItem("assistant", new List<ContentItem>{ new("output_text", "hi") });
        await Codex.RecordRolloutItemsAsync(rec, new[]{ item });
        var lines = File.ReadAllLines(rec.FilePath);
        Assert.True(lines.Length >= 2);
    }

    [Fact]
    public async Task HandlesNullRecorder()
    {
        await Codex.RecordRolloutItemsAsync(null, new[] { new MessageItem("assistant", new List<ContentItem>{ new("output_text", "hi") }) });
    }
}
