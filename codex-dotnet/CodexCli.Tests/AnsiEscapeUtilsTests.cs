using CodexCli.Util;
using Xunit;

public class AnsiEscapeUtilsTests
{
    [Fact]
    public void StripAnsi_RemovesEscapeSequences()
    {
        string input = "\u001b[31mRED\u001b[0m";
        string output = AnsiEscape.StripAnsi(input);
        Assert.Equal("RED", output);
    }
}
