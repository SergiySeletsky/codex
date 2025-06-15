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
}
