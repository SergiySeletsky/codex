using System.Net.Http;
using System.Text.Json;
using CodexCli.Util;
using CodexCli.Protocol;
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

    [Fact(Skip="flaky in CI")]
    public async Task ListCountLastAndClearMessages()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var task = server.RunAsync(cts.Token);
        await Task.Delay(100);
        using var http = new HttpClient();

        // add two messages
        var add1 = new JsonRpcMessage { Method = "messages/add", Id = JsonSerializer.SerializeToElement(1), Params = JsonDocument.Parse("{\"text\":\"a\"}").RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(add1)));
        var add2 = new JsonRpcMessage { Method = "messages/add", Id = JsonSerializer.SerializeToElement(2), Params = JsonDocument.Parse("{\"text\":\"b\"}").RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(add2)));

        // count
        var req = new JsonRpcMessage { Method = "messages/count", Id = JsonSerializer.SerializeToElement(3) };
        var resp = await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("2", body);

        // last
        req = new JsonRpcMessage { Method = "messages/last", Id = JsonSerializer.SerializeToElement(4), Params = JsonDocument.Parse("{\"count\":1}").RootElement };
        resp = await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("b", body);

        // list
        req = new JsonRpcMessage { Method = "messages/list", Id = JsonSerializer.SerializeToElement(5) };
        resp = await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("a", body);
        Assert.Contains("b", body);

        // search
        req = new JsonRpcMessage { Method = "messages/search", Id = JsonSerializer.SerializeToElement(6), Params = JsonDocument.Parse("{\"term\":\"b\"}").RootElement };
        resp = await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("b", body);

        // clear
        req = new JsonRpcMessage { Method = "messages/clear", Id = JsonSerializer.SerializeToElement(7) };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));

        req = new JsonRpcMessage { Method = "messages/count", Id = JsonSerializer.SerializeToElement(8) };
        resp = await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("0", body);

        cts.Cancel();
        await task;
    }
}
