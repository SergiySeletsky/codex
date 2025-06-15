using CodexCli.Config;
using CodexCli.Util;
using Xunit;

public class AppConfigMcpTests
{
    [Fact]
    public void LoadsMcpServers()
    {
        var toml = """
[mcp_servers.test]
command = "echo"
args = ["hi"]
[mcp_servers.test.env]
FOO = "bar"
""";
        var path = Path.GetTempFileName();
        File.WriteAllText(path, toml);
        var cfg = AppConfig.Load(path);
        Assert.True(cfg.McpServers.ContainsKey("test"));
        var sc = cfg.McpServers["test"];
        Assert.Equal("echo", sc.Command);
        Assert.Equal(new List<string>{"hi"}, sc.Args);
        Assert.NotNull(sc.Env);
        Assert.Equal("bar", sc.Env!["FOO"]);
    }
}
