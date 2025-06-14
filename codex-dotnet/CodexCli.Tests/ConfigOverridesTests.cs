using CodexCli.Config;
using Xunit;

public class ConfigOverridesTests
{
    [Fact]
    public void ParsesKeyValuePairs()
    {
        var ov = ConfigOverrides.Parse(new[]{"a=1","b=two"});
        Assert.Equal(1L, ov.Overrides["a"]);
        Assert.Equal("two", ov.Overrides["b"]);
    }

    [Fact]
    public void ParsesNestedValues()
    {
        var ov = ConfigOverrides.Parse(new[]{"nest.x=42"});
        Assert.Equal(42L, ov.Overrides["nest.x"]);
    }
}
