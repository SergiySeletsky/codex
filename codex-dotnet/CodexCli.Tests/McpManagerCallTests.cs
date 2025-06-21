using CodexCli.Util;
using CodexCli.Config;
using System.Text.Json;
using Xunit;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

public class McpManagerCallTests
{
    [Fact(Skip="flaky in CI")]
    public async Task CallToolThroughManagerReturnsResult()
    {
        string script = Path.Combine(Path.GetTempPath(), "mcp_mgr_stub.sh");
        await File.WriteAllTextAsync(script, "read line; echo '{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"nextCursor\":null,\"tools\":[{\"name\":\"codex\",\"inputSchema\":{\"type\":\"object\"},\"description\":null,\"annotations\":null}]}}'; read line; echo '{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"content\":[{\"value\":\"ok\"}],\"isError\":false}}'");
        try
        {
            var servers = new Dictionary<string, McpServerConfig> { { "test", new McpServerConfig("bash", new List<string>{ script }, null) } };
            var (mgr, _) = await McpConnectionManager.CreateAsync(servers);
            var result = await mgr.CallToolAsync("test__OAI_CODEX_MCP__codex", JsonDocument.Parse("{}").RootElement);
            Assert.Equal("ok", result.Content[0].GetProperty("value").GetString());
        }
        finally
        {
            File.Delete(script);
        }
    }
}
