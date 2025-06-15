using CodexCli.Models;
using CodexCli.Util;
using Xunit;
using System.Collections.Generic;
using System;

public class ExecRunnerOutputLimitTests
{
    [Fact]
    public async Task LimitsOutput()
    {
        var p = new ExecParams(new List<string>{"bash","-c","yes | head -n 1000"}, Directory.GetCurrentDirectory(), 1000, new(), 100, 5);
        var result = await ExecRunner.RunAsync(p, CancellationToken.None);
        Assert.True(result.Stdout.Split('\n').Length <= 5);
        Assert.True(result.Stdout.Length <= 100);
    }
}
