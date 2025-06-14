namespace CodexCli.Tests;

using CodexCli.Config;
using CodexCli.Commands;

public class AppConfigProfileTests
{
    [Fact]
    public void Load_WithProfile_AppliesProfileValues()
    {
        var toml = "model = 'gpt-4'\n" +
                    "[profiles.dev]\n" +
                    "model = 'gpt-3.5'\n" +
                    "model_provider = 'openai'\n" +
                    "hide_agent_reasoning = true\n" +
                    "disable_response_storage = true\n" +
                    "approval_policy = 'never'\n" +
                    "model_reasoning_effort = 'low'\n" +
                    "model_reasoning_summary = 'brief'\n";
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, toml);

        var cfg = AppConfig.Load(tmp, "dev");
        Assert.Equal("gpt-3.5", cfg.Model);
        Assert.Equal("openai", cfg.ModelProvider);
        Assert.Equal(ReasoningEffort.Low, cfg.ModelReasoningEffort);
        Assert.Equal(ReasoningSummary.Brief, cfg.ModelReasoningSummary);
    }
}
