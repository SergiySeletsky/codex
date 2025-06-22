using CodexCli.Config;
using CodexCli.Models;
using CodexCli.Util;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public class CodexRecordConversationItemsTests
{
    private static MessageItem Item() => new("assistant", new List<ContentItem>{ new("output_text", "hi") });

    [Fact]
    public async Task RecordsToTranscript()
    {
        var transcript = new ConversationHistory();
        await Codex.RecordConversationItemsAsync(null, transcript, new[]{ Item() });
        Assert.Single(transcript.Contents());
    }

    [Fact]
    public async Task RecordsToRollout()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var cfg = new AppConfig { CodexHome = dir };
        await using var rec = await RolloutRecorder.CreateAsync(cfg, "sess", null);
        await Codex.RecordConversationItemsAsync(rec, null, new[]{ Item() });
        var lines = File.ReadAllLines(rec.FilePath);
        Assert.True(lines.Length >= 2);
    }

    [Fact]
    public async Task RecordsToBoth()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var cfg = new AppConfig { CodexHome = dir };
        await using var rec = await RolloutRecorder.CreateAsync(cfg, "sess", null);
        var transcript = new ConversationHistory();
        await Codex.RecordConversationItemsAsync(rec, transcript, new[]{ Item() });
        var lines = File.ReadAllLines(rec.FilePath);
        Assert.True(lines.Length >= 2);
        Assert.Single(transcript.Contents());
    }
}
