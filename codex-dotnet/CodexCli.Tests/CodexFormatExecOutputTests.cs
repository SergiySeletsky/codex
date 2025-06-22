using CodexCli.Util;
using System.Text.Json;

public class CodexFormatExecOutputTests
{
    [Fact]
    public void FormatsJson()
    {
        var json = Codex.FormatExecOutput("hi", 0, TimeSpan.FromSeconds(1.23));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("hi", root.GetProperty("output").GetString());
        var meta = root.GetProperty("metadata");
        Assert.Equal(0, meta.GetProperty("exit_code").GetInt32());
        Assert.Equal(1.2, meta.GetProperty("duration_seconds").GetDouble(), 1);
    }
}
