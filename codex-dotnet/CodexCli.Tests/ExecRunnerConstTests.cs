using CodexCli.Util;
using Xunit;

public class ExecRunnerConstTests
{
    [Fact]
    public void NetworkEnvConstantSet()
    {
        Assert.Equal("CODEX_SANDBOX_NETWORK_DISABLED", ExecRunner.NetworkDisabledEnv);
    }

    [Fact]
    public void SessionEnvConstantSet()
    {
        Assert.Equal("CODEX_SESSION_ID", ExecRunner.SessionEnv);
    }

    [Fact]
    public void DefaultOutputConstantsSet()
    {
        Assert.Equal(10 * 1024, ExecRunner.DefaultMaxOutputBytes);
        Assert.Equal(256, ExecRunner.DefaultMaxOutputLines);
    }
}
