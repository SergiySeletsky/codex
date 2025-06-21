using CodexCli.Config;
using Xunit;

public class EnvFlagsTests
{
    [Fact]
    public void DefaultValuesReturned()
    {
        Environment.SetEnvironmentVariable("OPENAI_DEFAULT_MODEL", null);
        Assert.Equal("codex-mini-latest", EnvFlags.OPENAI_DEFAULT_MODEL);
    }

    [Fact]
    public void EnvironmentOverridesParsed()
    {
        Environment.SetEnvironmentVariable("OPENAI_TIMEOUT_MS", "500");
        Assert.Equal(TimeSpan.FromMilliseconds(500), EnvFlags.OPENAI_TIMEOUT_MS);
        Environment.SetEnvironmentVariable("OPENAI_TIMEOUT_MS", null);
    }
}
