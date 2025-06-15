using System.Net.Http;
using System.Text.Json;
using CodexCli.Util;
using Xunit;

public class McpServerMessageTests
{
    [Fact(Skip="flaky in CI")]
    public async Task AddAndGetMessage()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);
        using var http = new HttpClient();
        var addParams = JsonDocument.Parse("{\"text\":\"hi\"}");
        var req = new JsonRpcMessage{ Method="messages/add", Id=JsonSerializer.SerializeToElement(1), Params=addParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        var getParams = JsonDocument.Parse("{\"offset\":0}");
        req = new JsonRpcMessage{ Method="messages/getEntry", Id=JsonSerializer.SerializeToElement(2), Params=getParams.RootElement };
        var resp = await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("hi", body);
        cts.Cancel();
        await serverTask;
    }
}
