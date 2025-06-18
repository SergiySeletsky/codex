using CodexCli.Util;
using Xunit;

public class CitationRegexTests
{
    [Fact]
    public void RegexCapturesFileLine()
    {
        var m = CitationRegex.Instance.Match("See 【F:src/main.rs†L42-L99】");
        Assert.True(m.Success);
        Assert.Equal("src/main.rs", m.Groups[1].Value);
        Assert.Equal("42", m.Groups[2].Value);
        Assert.Equal("99", m.Groups[3].Value);
    }
}

