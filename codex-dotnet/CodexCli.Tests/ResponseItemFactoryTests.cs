using CodexCli.Models;
using CodexCli.Protocol;
using System.Collections.Generic;
using System.Text.Json;
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

    [Fact]
    public void MapsApprovalRequest()
    {
        var ev = new ExecApprovalRequestEvent("id", new List<string>{"rm","-rf","/"});
        var item = ResponseItemFactory.FromEvent(ev) as MessageItem;
        Assert.NotNull(item);
        Assert.Contains("Approve exec", item!.Content[0].Text);
    }

    [Fact]
    public void MapsResourceUpdate()
    {
        var ev = new ResourceUpdatedEvent("id", "file.txt");
        var item = ResponseItemFactory.FromEvent(ev) as MessageItem;
        Assert.NotNull(item);
        Assert.Contains("file.txt", item!.Content[0].Text);
    }

    [Fact]
    public void MapsProgressNotification()
    {
        var token = JsonDocument.Parse("0").RootElement;
        var ev = new ProgressNotificationEvent("id", "half", 0.5, token, 1.0);
        var item = ResponseItemFactory.FromEvent(ev) as MessageItem;
        Assert.NotNull(item);
        Assert.Contains("Progress", item!.Content[0].Text);
    }

    [Fact]
    public void MapsHistoryEntryResponse()
    {
        var ev = new GetHistoryEntryResponseEvent("id", "sess", 1, "hello");
        var item = ResponseItemFactory.FromEvent(ev) as MessageItem;
        Assert.NotNull(item);
        Assert.Contains("hello", item!.Content[0].Text);
    }

    [Fact]
    public void MapsPatchEvents()
    {
        var changes = new Dictionary<string, FileChange> { { "foo.txt", new AddFileChange("hi") } };
        var begin = new PatchApplyBeginEvent("id", false, changes);
        var beginItem = ResponseItemFactory.FromEvent(begin) as MessageItem;
        Assert.NotNull(beginItem);
        Assert.Contains("Applying patch", beginItem!.Content[0].Text);

        var end = new PatchApplyEndEvent("id", "", "", true);
        var endItem = ResponseItemFactory.FromEvent(end) as MessageItem;
        Assert.NotNull(endItem);
        Assert.Contains("Patch applied", endItem!.Content[0].Text);
    }

    [Fact]
    public void MapsExecEnd()
    {
        var ev = new ExecCommandEndEvent("id", "out", "", 0);
        var item = ResponseItemFactory.FromEvent(ev) as LocalShellCallItem;
        Assert.NotNull(item);
        Assert.Equal(LocalShellStatus.Completed, item!.Status);
    }

    [Fact]
    public void MapsMcpEvents()
    {
        var begin = new McpToolCallBeginEvent("c1", "srv", "tool", "{\"a\":1}");
        var bItem = ResponseItemFactory.FromEvent(begin) as FunctionCallItem;
        Assert.NotNull(bItem);
        Assert.Equal("tool", bItem!.Name);
        var end = new McpToolCallEndEvent("c1", true, "{\"ok\":true}");
        var eItem = ResponseItemFactory.FromEvent(end) as FunctionCallOutputItem;
        Assert.NotNull(eItem);
        Assert.Equal("c1", eItem!.CallId);
    }
}
