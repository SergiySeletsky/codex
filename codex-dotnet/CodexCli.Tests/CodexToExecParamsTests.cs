using CodexCli.Util;
using CodexCli.Models;
using CodexCli.Protocol;
using Xunit;
using System.IO;
using System.Collections.Generic;

public class CodexToExecParamsTests
{
    [Fact]
    public void ConvertsShellParams()
    {
        var cwd = Path.Combine(Path.GetTempPath(), "exectest");
        Directory.CreateDirectory(cwd);
        var policy = new ShellEnvironmentPolicy();
        policy.Set = new Dictionary<string,string>{{"FOO","BAR"}};
        var shell = new ShellToolCallParams(new List<string>{"echo","hi"}, "sub", 5);
        var exec = Codex.ToExecParams(shell, policy, cwd);
        Assert.Equal(Path.GetFullPath(Path.Combine(cwd, "sub")), exec.Cwd);
        Assert.Equal(new[]{"echo","hi"}, exec.Command);
        Assert.Equal(5, exec.TimeoutMs);
        Assert.Equal("BAR", exec.Env["FOO"]);
    }
}
