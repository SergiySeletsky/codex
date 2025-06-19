using CodexCli.Util;
using Xunit;
using System;
using System.IO;

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
        Assert.Equal("<image 16x16>", ToolResultUtils.FormatImageInfo(json));
    }

    [Fact]
    public void FormatImageInfo_JpegDimensions()
    {
        const string jpegBase64 = "/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAAEBAQEBAQEBAQEBAQECAgICAgQDAgICAgoKCgoKCgoKCgsLCwsMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAz/wAALCAAQABABAREA/8QAFQABAQAAAAAAAAAAAAAAAAAAAAf/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAgP/xAAVEQEBAAAAAAAAAAAAAAAAAAAAEf/aAAwDAQACEQMRAD8A0wD/2Q==";
        string json = $"{{\"content\":[{{\"type\":\"image\",\"data\":\"{jpegBase64}\"}}]}}";
        Assert.Equal("<image 16x16>", ToolResultUtils.FormatImageInfo(json));
    }

    [Fact]
    public void FormatImageInfoFromFile_ReturnsDimensions()
    {
        var path = Path.GetTempFileName() + ".png";
        File.WriteAllBytes(path, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAADUlEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="));
        Assert.Equal("<image 16x16>", ToolResultUtils.FormatImageInfoFromFile(path));
    }

    [Fact]
    public void FormatImageInfoFromFile_JpegDimensions()
    {
        var path = Path.GetTempFileName() + ".jpg";
        File.WriteAllBytes(path, Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDi6KKK+ZP3E//Z"));
        Assert.Equal("<image 16x16>", ToolResultUtils.FormatImageInfoFromFile(path));
    }
}
