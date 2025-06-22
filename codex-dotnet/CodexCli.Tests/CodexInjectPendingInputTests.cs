using CodexCli.Util;
using CodexCli.Protocol;
using CodexCli.Models;
using System.Collections.Generic;
using Xunit;

public class CodexInjectPendingInputTests
{
    [Fact]
    public void InjectInput_WhenTaskRunningQueuesInput()
    {
        var state = new CodexState { HasCurrentTask = true };
        var ok = Codex.InjectInput(state, new List<InputItem>{ new TextInputItem("hi") });
        Assert.True(ok);
        Assert.Single(state.PendingInput);
        var item = Assert.IsType<MessageInputItem>(state.PendingInput[0]);
        Assert.Equal("user", item.Role);
        Assert.Equal("hi", item.Content[0].Text);
    }

    [Fact]
    public void InjectInput_WhenNoTaskReturnsFalse()
    {
        var state = new CodexState { HasCurrentTask = false };
        var ok = Codex.InjectInput(state, new List<InputItem>{ new TextInputItem("hi") });
        Assert.False(ok);
        Assert.Empty(state.PendingInput);
    }

    [Fact]
    public void GetPendingInput_ReturnsAndClears()
    {
        var state = new CodexState { HasCurrentTask = true };
        Codex.InjectInput(state, new List<InputItem>{ new TextInputItem("hi") });
        var items = Codex.GetPendingInput(state);
        Assert.Single(items);
        Assert.Empty(state.PendingInput);
    }
}
