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
}
