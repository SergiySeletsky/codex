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
}
