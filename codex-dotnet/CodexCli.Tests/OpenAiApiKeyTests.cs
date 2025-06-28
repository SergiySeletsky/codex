using CodexCli.Util;
using CodexCli.Config;
using Xunit;

public class OpenAiApiKeyTests
{
    [Fact(Skip="requires env variable access in CI")]
    public void ReadsValueFromEnvironment()
    {
        Environment.SetEnvironmentVariable(OpenAiApiKey.EnvVar, "abc");
        // initialize static field
        var val = OpenAiApiKey.Get();
        Environment.SetEnvironmentVariable(OpenAiApiKey.EnvVar, null);
        Assert.Equal("abc", val);
    }

    [Fact]
    public void SetOverridesEnvironment()
    {
        Environment.SetEnvironmentVariable(OpenAiApiKey.EnvVar, "abc");
        OpenAiApiKey.Set("xyz");
        var val = OpenAiApiKey.Get();
        Environment.SetEnvironmentVariable(OpenAiApiKey.EnvVar, null);
        Assert.Equal("xyz", val);
    }

    [Fact]
    public void ApiKeyManagerUsesCache()
    {
        var provider = ModelProviderInfo.BuiltIns["openai"];
        OpenAiApiKey.Set("cached");
        var val = ApiKeyManager.GetKey(provider);
        OpenAiApiKey.Set(string.Empty);
        Assert.Equal("cached", val);
    }
}
