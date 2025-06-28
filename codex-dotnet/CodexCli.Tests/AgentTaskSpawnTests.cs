using CodexCli.Util;
using CodexCli.Protocol;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Xunit;

public class AgentTaskSpawnTests
{
    private static async IAsyncEnumerable<Event> SingleMessage()
    {
        yield return new AgentMessageEvent("x", "hi");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SpawnForwardsEvents()
    {
        var ch = Channel.CreateUnbounded<Event>();
        var task = AgentTask.Spawn(ch.Writer, "id", SingleMessage());
        var list = new List<Event>();
        for (int i = 0; i < 3; i++)
        {
            var ev = await ch.Reader.ReadAsync();
            list.Add(ev);
        }
        await task.RunningTask!;
        Assert.IsType<TaskStartedEvent>(list[0]);
        Assert.IsType<AgentMessageEvent>(list[1]);
        Assert.IsType<TaskCompleteEvent>(list[2]);
    }

    private static async IAsyncEnumerable<Event> Endless(CancellationToken token = default)
    {
        while (true)
        {
            await Task.Delay(50, token);
            yield return new AgentMessageEvent("x", "tick");
        }
    }

    [Fact]
    public async Task AbortSendsErrorEvent()
    {
        var ch = Channel.CreateUnbounded<Event>();
        var at = AgentTask.Spawn(ch.Writer, "id", Endless());
        var started = await ch.Reader.ReadAsync();
        Assert.IsType<TaskStartedEvent>(started);
        at.Abort();
        var err = await ch.Reader.ReadAsync();
        Assert.IsType<ErrorEvent>(err);
    }
}
