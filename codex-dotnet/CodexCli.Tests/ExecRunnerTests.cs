using CodexCli.Util;
using CodexCli.Models;
using Xunit;

public class ExecRunnerTests
{
    [Fact]
    public async Task RunEchoCommand()
    {
        var p = new ExecParams(new List<string>{"echo","hello"}, Directory.GetCurrentDirectory(), 1000, new());
        var result = await ExecRunner.RunAsync(p, CancellationToken.None);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello", result.Stdout);
    }
}
