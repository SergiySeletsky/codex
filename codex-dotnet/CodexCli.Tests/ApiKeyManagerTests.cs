using CodexCli.Util;
using CodexCli.Config;
using Xunit;

public class ApiKeyManagerTests
{
    [Fact]
    public void GetKeyFallsBackToDefaultEnv()
    {
        Environment.SetEnvironmentVariable(ApiKeyManager.DefaultEnvKey, "abc");
        var provider = new ModelProviderInfo { EnvKey = null };
        var key = ApiKeyManager.GetKey(provider);
        Environment.SetEnvironmentVariable(ApiKeyManager.DefaultEnvKey, null);
        Assert.Equal("abc", key);
    }
}
