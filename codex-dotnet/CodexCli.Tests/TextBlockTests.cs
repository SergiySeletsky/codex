using Xunit;
using CodexCli.Interactive;

public class TextBlockTests
{
    [Fact]
    public void HeightCountsWrappedLines()
    {
        var block = new TextBlock(new[] { "abcdef", "gh" });
        Assert.Equal(3, block.Height(3));
    }

    [Fact]
    public void RenderWindowReturnsLines()
    {
        var block = new TextBlock(new[] { "abcdef", "gh" });
        var window = block.RenderWindow(1, 2, 3).ToArray();
        Assert.Equal(new[] { "def", "gh" }, window);
    }
}
