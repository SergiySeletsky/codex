using CodexCli.Util;
using CodexCli.Models;
using CodexCli.Config;
using System.IO;
using Xunit;

public class CodexParseContainerExecArgumentsTests
{
    [Fact]
    public void ParsesArguments()
    {
        var json = "{\"command\":[\"echo\",\"hi\"],\"workdir\":\"sub\",\"timeout_ms\":5}";
        var policy = new ShellEnvironmentPolicy();
        bool ok = Codex.TryParseContainerExecArguments(json, policy, "/tmp", "123", out var execParams, out var err);
        Assert.True(ok);
        Assert.Null(err);
        Assert.NotNull(execParams);
        Assert.Equal(new[]{"echo","hi"}, execParams!.Command);
        Assert.Equal(Path.GetFullPath(Path.Combine("/tmp","sub")), execParams.Cwd);
        Assert.Equal(5, execParams.TimeoutMs);
        Assert.NotEmpty(execParams.Env);
    }

    [Fact]
    public void ReturnsErrorOnFailure()
    {
        var policy = new ShellEnvironmentPolicy();
        bool ok = Codex.TryParseContainerExecArguments("not json", policy, "/tmp", "call", out var execParams, out var err);
        Assert.False(ok);
        Assert.Null(execParams);
        Assert.NotNull(err);
    }
}
