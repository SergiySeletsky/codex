using System.Net.Http;
using System.Text.Json;
using CodexCli.Util;
using CodexCli.Protocol;
using Xunit;

public class McpServerTests
{
    [Fact]
    public async Task InitializeAndListTools()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100); // wait for listener
        using var client = new HttpClient();
        var req = new JsonRpcMessage { Method = "initialize", Id = JsonSerializer.SerializeToElement(1) };
        var resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("codex-mcp", body);
        req = new JsonRpcMessage { Method = "tools/list", Id = JsonSerializer.SerializeToElement(2) };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("codex", body);
        req = new JsonRpcMessage { Method = "prompts/list", Id = JsonSerializer.SerializeToElement(3) };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("prompts", body);
        cts.Cancel();
        await serverTask;
    }
}
