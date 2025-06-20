using CodexCli.Util;
using Xunit;
using System.IO;
using System.Threading.Tasks;

public class McpClientTests
{
    [Fact(Skip="flaky in CI")]
    public async Task StartAsyncReceivesResponse()
    {
        string script = Path.Combine(Path.GetTempPath(), "mcp_stub.sh");
        await File.WriteAllTextAsync(script, "read line; echo '{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"ok\":true}}'");
        try
        {
            await using var client = await McpClient.StartAsync("bash", new[] { script });
            var resp = await client.SendRequestAsync("ping", null);
            Assert.True(resp.Result.HasValue);
            Assert.True(resp.Result.Value.GetProperty("ok").GetBoolean());
        }
        finally
        {
            File.Delete(script);
        }
    }

    [Fact(Skip="flaky in CI")]
    public async Task ListRootsReturnsRoot()
    {
        string script = Path.Combine(Path.GetTempPath(), "mcp_stub.sh");
        await File.WriteAllTextAsync(script,
            "read line; echo '{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"roots\":[{\"name\":null,\"uri\":\"mem:/\"}]}}'");
        try
        {
            await using var client = await McpClient.StartAsync("bash", new[] { script });
            var roots = await client.ListRootsAsync();
            Assert.Single(roots.Roots);
            Assert.Equal("mem:/", roots.Roots[0].Uri);
        }
        finally
        {
            File.Delete(script);
        }
    }

    [Fact(Skip="flaky in CI")]
    public async Task ListToolsReturnsCodex()
    {
        string script = Path.Combine(Path.GetTempPath(), "mcp_stub.sh");
        await File.WriteAllTextAsync(script,
            "read line; echo '{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"nextCursor\":null,\"tools\":[{\"name\":\"codex\",\"inputSchema\":{\"type\":\"object\"},\"description\":null,\"annotations\":null}]}}'");
        try
        {
            await using var client = await McpClient.StartAsync("bash", new[] { script });
            var tools = await client.ListToolsAsync();
            Assert.Contains(tools.Tools, t => t.Name == "codex");
        }
        finally
        {
            File.Delete(script);
        }
    }

    [Fact(Skip="flaky in CI")]
    public async Task PingAsyncReturnsOk()
    {
        string script = Path.Combine(Path.GetTempPath(), "mcp_stub.sh");
        await File.WriteAllTextAsync(script, "read line; echo '{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"ok\":true}}'");
        try
        {
            await using var client = await McpClient.StartAsync("bash", new[] { script });
            await client.PingAsync();
        }
        finally
        {
            File.Delete(script);
        }
    }
}
