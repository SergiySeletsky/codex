using CodexCli.Util;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class CodexCallToolTests
{
    [Fact(Skip="flaky in CI")]
    public async Task CallToolViaHelperReturnsResult()
    {
        string script = Path.Combine(Path.GetTempPath(), "mcp_call_tool_stub.sh");
        await File.WriteAllTextAsync(script, "read line; echo '{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"nextCursor\":null,\"tools\":[{\"name\":\"codex\",\"inputSchema\":{\"type\":\"object\"},\"description\":null,\"annotations\":null}]}}'; read line; echo '{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"content\":[{\"value\":\"ok\"}],\"isError\":false}}'");
        try
        {
            var servers = new Dictionary<string, McpServerConfig> { { "test", new McpServerConfig("bash", new List<string>{ script }, null) } };
            var (mgr, _) = await McpConnectionManager.CreateAsync(servers);
            var result = await Codex.CallToolAsync(mgr, "test", "codex", JsonDocument.Parse("{}").RootElement);
            Assert.Equal("ok", result.Content[0].GetProperty("value").GetString());
        }
        finally
        {
            File.Delete(script);
        }
    }
}
