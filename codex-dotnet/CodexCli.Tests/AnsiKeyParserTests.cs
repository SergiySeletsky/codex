using CodexCli.Interactive;
using Xunit;

public class AnsiKeyParserTests
{
    [Fact]
    public void ParsesArrowKeys()
    {
        var parser = new AnsiKeyParser();
        ConsoleKeyInfo key;
        Assert.True(parser.ProcessChar('\u001b', out key));
        Assert.True(parser.ProcessChar('[', out key));
        Assert.True(parser.ProcessChar('D', out key));
        Assert.Equal(ConsoleKey.LeftArrow, key.Key);
    }
}
