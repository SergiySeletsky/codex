using CodexCli.Util;
using CodexCli.Config;
using Xunit;

public class ApiKeyManagerTests
{
    [Fact(Skip="requires env write access")]
    public void GetKeyFallsBackToDefaultEnv()
    {
        Environment.SetEnvironmentVariable(ApiKeyManager.DefaultEnvKey, "abc");
        var provider = new ModelProviderInfo { EnvKey = null };
        var key = ApiKeyManager.GetKey(provider);
        Environment.SetEnvironmentVariable(ApiKeyManager.DefaultEnvKey, null);
        Assert.Equal("abc", key);
    }

    [Fact(Skip="requires env write access")]
    public void PrintEnvInstructionsPrefersProvider()
    {
        var provider = new ModelProviderInfo { EnvKeyInstructions = "set FOO" };
        using var sw = new StringWriter();
        var orig = Console.Out;
        Console.SetOut(sw);
        ApiKeyManager.PrintEnvInstructions(provider);
        Console.SetOut(orig);
        Assert.Contains("FOO", sw.ToString());
    }

    [Fact(Skip="requires env write access")]
    public void GetKeyUsesProviderEnvVar()
    {
        var provider = new ModelProviderInfo { EnvKey = "TEST_KEY" };
        Environment.SetEnvironmentVariable("TEST_KEY", "v1");
        var key = ApiKeyManager.GetKey(provider);
        Environment.SetEnvironmentVariable("TEST_KEY", null);
        Assert.Equal("v1", key);
    }
}
