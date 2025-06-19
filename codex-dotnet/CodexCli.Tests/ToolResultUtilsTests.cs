using CodexCli.Util;
using Xunit;

public class ToolResultUtilsTests
{
    [Fact]
    public void HasImageOutput_DetectsImage()
    {
        string json = "{\"content\":[{\"type\":\"image\",\"data\":\"abc\"}]}";
        Assert.True(ToolResultUtils.HasImageOutput(json));
    }

    [Fact]
    public void HasImageOutput_IgnoresTextOnly()
    {
        string json = "{\"content\":[{\"type\":\"text\",\"data\":\"hi\"}]}";
        Assert.False(ToolResultUtils.HasImageOutput(json));
    }

    [Fact]
    public void FormatImageInfo_ReturnsDimensions()
    {
        string json = "{\"content\":[{\"type\":\"image\",\"data\":\"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAADUlEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg==\"}]}";
        Assert.Equal("<image 1x1>", ToolResultUtils.FormatImageInfo(json));
    }
}
