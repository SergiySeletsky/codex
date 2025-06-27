using CodexCli.Protocol;
using CodexCli.Models;
using CodexCli.Util;
using CodexCli.Config;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

public class RealCodexAgentRolloutTests
{
    [Fact]
    public async Task RecordsRollout()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var cfg = new AppConfig { CodexHome = dir };
        await using var rec = await RolloutRecorder.CreateAsync(cfg, "sess", null);
        var events = new List<Event>();
        var content = "event: response.output_item.done\n" +
                      "data: {\"type\":\"response.output_item.done\",\"item\":{\"type\":\"message\",\"role\":\"assistant\",\"content\":[{\"type\":\"text\",\"text\":\"hi\"}]}}\n\n" +
                      "event: response.completed\n" +
                      "data: {\"type\":\"response.completed\",\"response\":{\"id\":\"r1\",\"output\":[]}}\n\n";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        Environment.SetEnvironmentVariable("CODEX_RS_SSE_FIXTURE", path);
        await foreach (var ev in RealCodexAgent.RunWithRolloutAsync("hi", new OpenAIClient(null, "http://localhost"), "gpt-4", rec))
            events.Add(ev);
        Environment.SetEnvironmentVariable("CODEX_RS_SSE_FIXTURE", null);
        File.Delete(path);
        Assert.Contains(events, e => e is TaskCompleteEvent);
        var lines = File.ReadAllLines(rec.FilePath);
        Assert.True(lines.Length >= 2);
    }
}
