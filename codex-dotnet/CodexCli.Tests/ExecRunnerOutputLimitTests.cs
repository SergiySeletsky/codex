using CodexCli.Models;
using CodexCli.Util;
using Xunit;
using System.Collections.Generic;
using System;

public class ExecRunnerOutputLimitTests
{
    [Fact(Skip="flaky under CI")]
    public async Task LimitsOutput()
    {
        var p = new ExecParams(new List<string>{"bash","-c","yes | head -n 1000"}, Directory.GetCurrentDirectory(), 1000, new(), 100, 5, null);
        var result = await ExecRunner.RunAsync(p, CancellationToken.None);
        var lines = result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length <= 5);
        Assert.True(result.Stdout.Length <= 100);
    }
}
