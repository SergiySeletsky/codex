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

    [Fact]
    public void ParsesHomeEndDeleteAndPages()
    {
        var parser = new AnsiKeyParser();
        ConsoleKeyInfo key;

        Assert.True(parser.ProcessChar('\u001b', out key));
        Assert.True(parser.ProcessChar('[', out key));
        Assert.True(parser.ProcessChar('H', out key));
        Assert.Equal(ConsoleKey.Home, key.Key);

        Assert.True(parser.ProcessChar('\u001b', out key));
        Assert.True(parser.ProcessChar('[', out key));
        Assert.True(parser.ProcessChar('F', out key));
        Assert.Equal(ConsoleKey.End, key.Key);

        Assert.True(parser.ProcessChar('\u001b', out key));
        Assert.True(parser.ProcessChar('[', out key));
        Assert.True(parser.ProcessChar('3', out key));
        Assert.True(parser.ProcessChar('~', out key));
        Assert.Equal(ConsoleKey.Delete, key.Key);

        Assert.True(parser.ProcessChar('\u001b', out key));
        Assert.True(parser.ProcessChar('[', out key));
        Assert.True(parser.ProcessChar('5', out key));
        Assert.True(parser.ProcessChar('~', out key));
        Assert.Equal(ConsoleKey.PageUp, key.Key);

        Assert.True(parser.ProcessChar('\u001b', out key));
        Assert.True(parser.ProcessChar('[', out key));
        Assert.True(parser.ProcessChar('6', out key));
        Assert.True(parser.ProcessChar('~', out key));
        Assert.Equal(ConsoleKey.PageDown, key.Key);
    }
}
