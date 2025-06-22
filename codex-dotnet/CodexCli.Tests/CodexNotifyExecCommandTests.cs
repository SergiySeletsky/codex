using CodexCli.Util;
using CodexCli.Protocol;
using CodexCli.Config;
using CodexCli.Models;
using Xunit;
using System.Collections.Generic;

public class CodexNotifyExecCommandTests
{
    [Fact]
    public void BeginEvent_HasExpectedFields()
    {
        var policy = new ShellEnvironmentPolicy();
        var p = new ExecParams(new List<string>{"echo","hi"}, "/tmp", null, ExecEnv.Create(policy));
        var ev = Codex.NotifyExecCommandBegin("id1", "call1", p);
        Assert.Equal("id1", ev.Id);
        Assert.Equal(new[]{"echo","hi"}, ev.Command);
        Assert.Equal("/tmp", ev.Cwd);
    }

    [Fact]
    public void EndEvent_TruncatesOutput()
    {
        var stdout = new string('a', 6000);
        var stderr = new string('b', 6000);
        var ev = Codex.NotifyExecCommandEnd("id2", "call2", stdout, stderr, 0);
        Assert.Equal(5120, ev.Stdout.Length);
        Assert.Equal(5120, ev.Stderr.Length);
        Assert.Equal(0, ev.ExitCode);
    }

    [Fact]
    public void BackgroundEvent_HasMessage()
    {
        var ev = Codex.NotifyBackgroundEvent("id3", "hello");
        Assert.Equal("id3", ev.Id);
        Assert.Equal("hello", ev.Message);
    }
}
