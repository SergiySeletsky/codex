using CodexCli.Config;
using CodexCli.Commands;
using System.CommandLine;
using System.CommandLine.Parsing;
using Xunit;

public class ProviderInfoTests
{
    [Fact]
    public void ProviderInfoPrintsDetails()
    {
        var cmd = ProviderCommand.Create(new Option<string?>("--config"));
        var parser = new Parser(cmd);
        var result = parser.Invoke("info openai");
        Assert.Equal(0, result);
    }

    [Fact]
    public void ProviderEnvKeyInstructions()
    {
        var info = ModelProviderInfo.BuiltIns["openai"];
        Assert.NotNull(info.EnvKeyInstructions);
    }
}
