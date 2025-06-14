using CodexCli.Config;
using Xunit;

public class ModelProviderTests
{
    [Fact]
    public void BuiltInsIncludeOpenRouter()
    {
        var info = ModelProviderInfo.BuiltIns["openrouter"];
        Assert.Equal("OpenRouter", info.Name);
        Assert.Equal("https://openrouter.ai/api/v1", info.BaseUrl);
    }

    [Fact]
    public void BuiltInsIncludeGemini()
    {
        var info = ModelProviderInfo.BuiltIns["gemini"];
        Assert.Equal("Gemini", info.Name);
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/openai", info.BaseUrl);
    }

    [Fact]
    public void LoadCustomProvider()
    {
        var toml = "model_providers.myprov.base_url = \"https://x.com\"\nmodel_providers.myprov.name = \"X\"\nmodel_providers.myprov.env_key_instructions = \"set X_API_KEY\"";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, toml);
        var cfg = AppConfig.Load(path);
        var info = cfg.ModelProviders["myprov"];
        Assert.Equal("X", info.Name);
        Assert.Equal("https://x.com", info.BaseUrl);
        Assert.Equal("set X_API_KEY", info.EnvKeyInstructions);
    }
}
