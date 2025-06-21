using CodexCli.Util;
using CodexCli.Models;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public class McpToolCallTests
{
    [Fact(Skip="flaky in CI")]
    public async Task HandleMcpToolCallSuccess()
    {
        string script = Path.Combine(Path.GetTempPath(), "mcp_tool_success.sh");
        await File.WriteAllTextAsync(script, "read line; echo '{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"content\":[{\"value\":\"ok\"}],\"isError\":false}}'");
        try
        {
            await using var client = await McpClient.StartAsync("bash", new[] { script });
            var res = await McpToolCall.HandleMcpToolCallAsync(client, "1", "codex", JsonDocument.Parse("{}").RootElement);
            var mcp = Assert.IsType<McpToolCallOutputInputItem>(res);
            Assert.Contains("ok", mcp.ResultJson);
        }
        finally
        {
            File.Delete(script);
        }
    }

    [Fact(Skip="flaky in CI")]
    public async Task HandleMcpToolCallError()
    {
        string script = Path.Combine(Path.GetTempPath(), "mcp_tool_err.sh");
        await File.WriteAllTextAsync(script, "read line; echo 'notjson'");
        try
        {
            await using var client = await McpClient.StartAsync("bash", new[] { script });
            var res = await McpToolCall.HandleMcpToolCallAsync(client, "2", "codex", null);
            var mcp = Assert.IsType<McpToolCallOutputInputItem>(res);
            Assert.Contains("error", mcp.ResultJson);
        }
        finally
        {
            File.Delete(script);
        }
    }
}
