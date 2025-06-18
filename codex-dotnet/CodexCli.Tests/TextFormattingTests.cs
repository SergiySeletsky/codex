using CodexCli.Util;
using Xunit;

public class TextFormattingTests
{
    [Fact]
    public void TruncateTextShorterThanLimit()
    {
        Assert.Equal("Hi", TextFormatting.TruncateText("Hi", 10));
    }

    [Fact]
    public void TruncateTextAddsEllipsis()
    {
        Assert.Equal("Hello...", TextFormatting.TruncateText("Hello, world!", 8));
    }

    [Fact]
    public void FormatJsonCompactSimpleObject()
    {
        string json = "{ \"name\": \"John\", \"age\": 30 }";
        Assert.Equal("{\"name\": \"John\", \"age\": 30}", TextFormatting.FormatJsonCompact(json));
    }

    [Fact]
    public void FormatAndTruncateToolResultFormatsJsonAndTruncates()
    {
        string json = "{\n  \"a\": 1,\n  \"b\": 2\n}";
        string result = TextFormatting.FormatAndTruncateToolResult(json, 1, 10);
        Assert.Equal("{\"a\": ...", result);
    }
}

