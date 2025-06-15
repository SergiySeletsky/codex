using CodexCli.Models;
using CodexCli.Protocol;
using CodexCli.Util;
using Xunit;

public class ExecRunnerNetworkTests
{
    [Fact]
    public async Task SetsNetworkDisabledVariable()
    {
        var policy = SandboxPolicy.NewReadOnlyPolicy();
        var p = new ExecParams(new List<string>{"bash","-c","echo -n $CODEX_SANDBOX_NETWORK_DISABLED"}, Directory.GetCurrentDirectory(), 1000, new());
        var result = await ExecRunner.RunAsync(p, CancellationToken.None, policy);
        Assert.Equal("1", result.Stdout.Trim());
    }
}
