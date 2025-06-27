using CodexCli.Models;
using CodexCli.Util;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public class ExecRunnerEnvVarTests
{
    [Fact]
    public async Task PassesEnvironmentVariables()
    {
        var env = new Dictionary<string,string>{{"FOO","bar"}};
        var p = new ExecParams(new List<string>{"bash","-c","echo -n $FOO"}, Directory.GetCurrentDirectory(), 1000, env, null, null, null);
        var result = await ExecRunner.RunAsync(p, CancellationToken.None);
        Assert.Equal("bar", result.Stdout.Trim());
    }
}

