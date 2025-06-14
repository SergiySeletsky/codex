using CodexCli.Util;
using System.Text.Json;
using Xunit;

public class JsonToTomlTests
{
    [Fact]
    public void Convert_Object()
    {
        var json = JsonDocument.Parse("{\"a\":1,\"b\":\"x\"}");
        var toml = JsonToToml.ConvertToToml(json.RootElement);
        Assert.Contains("a = 1", toml);
        Assert.Contains("b = \"x\"", toml);
    }

    [Fact]
    public void Convert_Array()
    {
        var json = JsonDocument.Parse("[1,2,3]");
        var toml = JsonToToml.ConvertToToml(json.RootElement);
        Assert.Contains("0 = 1", toml);
        Assert.Contains("1 = 2", toml);
    }
}
