using CodexCli.Config;
using Xunit;

public class ConfigOverridesTests
{
    [Fact]
    public void ParsesKeyValuePairs()
    {
        var ov = ConfigOverrides.Parse(new[]{"a=1","b=two"});
        Assert.Equal("1", ov.Overrides["a"]);
        Assert.Equal("two", ov.Overrides["b"]);
    }
}
