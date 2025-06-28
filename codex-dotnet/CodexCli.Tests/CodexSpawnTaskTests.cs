using CodexCli.Util;
using CodexCli.Protocol;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

public class CodexSpawnTaskTests
{
    private static async IAsyncEnumerable<Event> Single()
    {
        yield return new AgentMessageEvent("x", "hi");
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SpawnTaskSetsStateAndForwardsEvents()
    {
        var ch = Channel.CreateUnbounded<Event>();
        var state = new CodexState();
        var task = Codex.SpawnTask(state, ch.Writer, "id", Single());
        var started = await ch.Reader.ReadAsync();
        Assert.IsType<TaskStartedEvent>(started);
        Assert.Equal(task, state.CurrentTask);
        Assert.True(state.HasCurrentTask);
    }
}
