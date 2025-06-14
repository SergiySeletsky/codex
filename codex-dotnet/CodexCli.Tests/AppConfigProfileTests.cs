namespace CodexCli.Tests;

using CodexCli.Config;
using CodexCli.Commands;

public class AppConfigProfileTests
{
    [Fact]
    public void Load_WithProfile_AppliesProfileValues()
    {
        var toml = @"model = 'gpt-4'
[profiles.dev]
model = 'gpt-3.5'
model_provider = 'openai'
";
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, toml);

        var cfg = AppConfig.Load(tmp, "dev");
        Assert.Equal("gpt-3.5", cfg.Model);
        Assert.Equal("openai", cfg.ModelProvider);
    }
}
