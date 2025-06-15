using CodexCli.Models;
using CodexCli.Protocol;
using System.Collections.Generic;
using Xunit;

public class ResponseItemFactoryTests
{
    [Fact]
    public void MapsAgentMessage()
    {
        var ev = new AgentMessageEvent("1","hi");
        var item = ResponseItemFactory.FromEvent(ev) as MessageItem;
        Assert.NotNull(item);
        Assert.Equal("assistant", item!.Role);
    }

    [Fact]
    public void MapsExecBegin()
    {
        var ev = new ExecCommandBeginEvent("call1", new List<string>{"ls"}, "/");
        var item = ResponseItemFactory.FromEvent(ev) as LocalShellCallItem;
        Assert.NotNull(item);
        Assert.Equal(LocalShellStatus.InProgress, item!.Status);
    }

    [Fact]
    public void MapsTaskComplete()
    {
        var ev = new TaskCompleteEvent("id", "done");
        var item = ResponseItemFactory.FromEvent(ev) as MessageItem;
        Assert.NotNull(item);
        Assert.Equal("assistant", item!.Role);
        Assert.Contains("done", item.Content[0].Text);
    }

    [Fact]
    public void MapsTaskStarted()
    {
        var ev = new TaskStartedEvent("id");
        var item = ResponseItemFactory.FromEvent(ev);
        Assert.IsType<OtherItem>(item);
    }
}
