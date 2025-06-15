using CodexCli.Util;
using Xunit;

public class ExecRunnerConstTests
{
    [Fact]
    public void NetworkEnvConstantSet()
    {
        Assert.Equal("CODEX_SANDBOX_NETWORK_DISABLED", ExecRunner.NetworkDisabledEnv);
    }
}
