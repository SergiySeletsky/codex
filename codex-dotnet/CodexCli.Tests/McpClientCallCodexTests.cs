using CodexCli.Util;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

public class McpClientCallCodexTests
{
    [Fact(Skip="flaky in CI")]
    public async Task CallCodexReturnsResult()
    {
        string script = Path.Combine(Path.GetTempPath(), "mcp_stub.sh");
        await File.WriteAllTextAsync(script, "read line; echo '{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"content\":[{\"value\":\"ok\"}],\"isError\":false}}'");
        try
        {
            await using var client = await McpClient.StartAsync("bash", new[] { script });
            var result = await client.CallCodexAsync(new CodexToolCallParam("hi"));
            Assert.Equal("ok", result.Content[0].GetProperty("value").GetString());
        }
        finally
        {
            File.Delete(script);
        }
    }
}
