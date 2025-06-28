using CodexCli.Protocol;
using CodexCli.Util;
using CodexCli.Config;
using System.Threading.Channels;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class CodexSubmissionLoopTests
{
    private static async IAsyncEnumerable<Event> SimpleAgent(string prompt, CancellationToken cancel)
    {
        yield return new TaskStartedEvent("sub");
        yield return new TaskCompleteEvent("sub", prompt);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task LoopSpawnsTasks()
    {
        var subs = Channel.CreateUnbounded<Submission>();
        var evs = Channel.CreateUnbounded<Event>();
        var loop = Codex.RunSubmissionLoopAsync(subs.Reader, evs.Writer, SimpleAgent);
        await subs.Writer.WriteAsync(new Submission("1", new ConfigureSessionOp(ModelProviderInfo.BuiltIns["mock"], "gpt-4", "hi", null, "/tmp")));
        subs.Writer.Complete();
        var received = new List<Event>();
        await foreach (var ev in evs.Reader.ReadAllAsync())
            received.Add(ev);
        await loop;
        Assert.Contains(received, e => e is TaskStartedEvent);
        Assert.Contains(received, e => e is TaskCompleteEvent);
    }
}
