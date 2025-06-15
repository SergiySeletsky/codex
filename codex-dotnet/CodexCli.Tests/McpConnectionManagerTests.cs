using CodexCli.Util;
using Xunit;

public class McpConnectionManagerTests
{
    [Fact]
    public void ParseFqNameRoundTrip()
    {
        var fq = McpConnectionManager.FullyQualifiedToolName("srv","tool");
        Assert.True(McpConnectionManager.TryParseFullyQualifiedToolName(fq, out var s, out var t));
        Assert.Equal("srv", s);
        Assert.Equal("tool", t);
    }

    [Fact]
    public void ParseFqNameInvalid()
    {
        Assert.False(McpConnectionManager.TryParseFullyQualifiedToolName("badname", out _, out _));
    }
}
