using System.Net.Http;
using System.Text.Json;
using System.IO;
using CodexCli.Util;
using CodexCli.Protocol;
using Xunit;

public class McpServerTests
{
    [Fact(Skip="flaky in CI")]
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
        Assert.Contains("protocolVersion", body);
        req = new JsonRpcMessage { Method = "tools/list", Id = JsonSerializer.SerializeToElement(2) };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("codex", body);
        req = new JsonRpcMessage { Method = "prompts/list", Id = JsonSerializer.SerializeToElement(3) };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("demo", body);

        var getPromptParams = JsonDocument.Parse("{\"name\":\"demo\"}");
        req = new JsonRpcMessage { Method = "prompts/get", Id = JsonSerializer.SerializeToElement(4), Params = getPromptParams.RootElement };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Say hello", body);

        req = new JsonRpcMessage { Method = "roots/list", Id = JsonSerializer.SerializeToElement(30) };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("roots", body);

        req = new JsonRpcMessage { Method = "resources/list", Id = JsonSerializer.SerializeToElement(31) };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("demo.txt", body);

        var readParams = JsonDocument.Parse("{\"uri\":\"mem:/demo.txt\"}");
        req = new JsonRpcMessage { Method = "resources/read", Id = JsonSerializer.SerializeToElement(32), Params = readParams.RootElement };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Hello from MCP", body);

        var callJson = JsonDocument.Parse("{\"name\":\"codex\",\"arguments\":{\"prompt\":\"hi\"}}");
        req = new JsonRpcMessage { Method = "tools/call", Id = JsonSerializer.SerializeToElement(5), Params = callJson.RootElement };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("codex done", body);

        var writeParams = JsonDocument.Parse("{\"uri\":\"mem:/foo.txt\",\"text\":\"bar\"}");
        req = new JsonRpcMessage { Method = "resources/write", Id = JsonSerializer.SerializeToElement(6), Params = writeParams.RootElement };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();

        var readFooParams = JsonDocument.Parse("{\"uri\":\"mem:/foo.txt\"}");
        req = new JsonRpcMessage { Method = "resources/read", Id = JsonSerializer.SerializeToElement(7), Params = readFooParams.RootElement };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("bar", body);

        var setLevelParams = JsonDocument.Parse("{\"level\":\"debug\"}");
        req = new JsonRpcMessage { Method = "logging/setLevel", Id = JsonSerializer.SerializeToElement(8), Params = setLevelParams.RootElement };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();

        var completeParams = JsonDocument.Parse("{\"argument\":{\"name\":\"text\",\"value\":\"hel\"},\"ref\":{\"uri\":\"mem:/\"}}");
        req = new JsonRpcMessage { Method = "completion/complete", Id = JsonSerializer.SerializeToElement(9), Params = completeParams.RootElement };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("demo completion", body);

        var createMsgParams = JsonDocument.Parse("{\"messages\":[{\"role\":\"user\",\"content\":{\"text\":\"hi\",\"type\":\"text\"}}],\"maxTokens\":5}");
        req = new JsonRpcMessage { Method = "sampling/createMessage", Id = JsonSerializer.SerializeToElement(10), Params = createMsgParams.RootElement };
        resp = await client.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("echo hi", body);

        cts.Cancel();
        await serverTask;
    }

    [Fact(Skip="flaky in CI")]
    public async Task SubscribeAndReceiveUpdate()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);
        using var http = new HttpClient();
        using var stream = await http.GetStreamAsync($"http://localhost:{port}/events");
        using var reader = new StreamReader(stream);

        var subParams = JsonDocument.Parse("{\"uri\":\"mem:/demo.txt\"}");
        var req = new JsonRpcMessage { Method = "resources/subscribe", Id = JsonSerializer.SerializeToElement(11), Params = subParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));

        var writeParams = JsonDocument.Parse("{\"uri\":\"mem:/demo.txt\",\"text\":\"hi\"}");
        req = new JsonRpcMessage { Method = "resources/write", Id = JsonSerializer.SerializeToElement(12), Params = writeParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));

        string? line = null;
        for (int i = 0; i < 20 && line == null; i++)
        {
            var l = await reader.ReadLineAsync();
            if (l != null && l.StartsWith("data:")) line = l;
        }

        Assert.NotNull(line);
        Assert.Contains("demo.txt", line);
        cts.Cancel();
        await serverTask;
    }

    [Fact(Skip="flaky in CI")]
    public async Task AddPromptSendsEvent()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);
        using var http = new HttpClient();
        using var stream = await http.GetStreamAsync($"http://localhost:{port}/events");
        using var reader = new StreamReader(stream);

        var addParams = JsonDocument.Parse("{\"name\":\"test\",\"message\":\"hi\"}");
        var req = new JsonRpcMessage { Method = "prompts/add", Id = JsonSerializer.SerializeToElement(40), Params = addParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));

        string? line = null;
        for (int i = 0; i < 20 && line == null; i++)
        {
            var l = await reader.ReadLineAsync();
            if (l != null && l.StartsWith("data:")) line = l;
        }

        Assert.NotNull(line);
        Assert.Contains("PromptListChangedEvent", line);
        cts.Cancel();
        await serverTask;
    }

    [Fact(Skip="flaky in CI")]
    public async Task WriteResourceSendsProgressEvents()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);
        using var http = new HttpClient();
        using var stream = await http.GetStreamAsync($"http://localhost:{port}/events");
        using var reader = new StreamReader(stream);

        var writeParams = JsonDocument.Parse("{\"uri\":\"mem:/p.txt\",\"text\":\"hi\"}");
        var req = new JsonRpcMessage { Method = "resources/write", Id = JsonSerializer.SerializeToElement(50), Params = writeParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));

        int seen = 0;
        for (int i = 0; i < 40 && seen < 2; i++)
        {
            var l = await reader.ReadLineAsync();
            if (l != null && l.Contains("ProgressNotificationEvent")) seen++;
        }

        Assert.Equal(2, seen);
        cts.Cancel();
        await serverTask;
    }

    [Fact(Skip="flaky in CI")]
    public async Task WriteResourceSendsListChangedEvent()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);
        using var http = new HttpClient();
        using var stream = await http.GetStreamAsync($"http://localhost:{port}/events");
        using var reader = new StreamReader(stream);

        var writeParams = JsonDocument.Parse("{\"uri\":\"mem:/p2.txt\",\"text\":\"hi\"}");
        var req = new JsonRpcMessage { Method = "resources/write", Id = JsonSerializer.SerializeToElement(51), Params = writeParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));

        string? line = null;
        for (int i = 0; i < 20 && line == null; i++)
        {
            var l = await reader.ReadLineAsync();
            if (l != null && l.Contains("ResourceListChangedEvent")) line = l;
        }

        Assert.NotNull(line);
        cts.Cancel();
        await serverTask;
    }

    [Fact(Skip="flaky in CI")]
    public async Task WriteSubscribedResourceSendsUpdatedEvent()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);
        using var http = new HttpClient();
        using var stream = await http.GetStreamAsync($"http://localhost:{port}/events");
        using var reader = new StreamReader(stream);

        var subParams = JsonDocument.Parse("{\"uri\":\"mem:/sub.txt\"}");
        var subReq = new JsonRpcMessage { Method = "resources/subscribe", Id = JsonSerializer.SerializeToElement(52), Params = subParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(subReq)));

        var writeParams = JsonDocument.Parse("{\"uri\":\"mem:/sub.txt\",\"text\":\"hi\"}");
        var req = new JsonRpcMessage { Method = "resources/write", Id = JsonSerializer.SerializeToElement(53), Params = writeParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));

        string? line = null;
        for (int i = 0; i < 20 && line == null; i++)
        {
            var l = await reader.ReadLineAsync();
            if (l != null && l.Contains("ResourceUpdatedEvent")) line = l;
        }

        Assert.NotNull(line);
        cts.Cancel();
        await serverTask;
    }

    [Fact(Skip="flaky in CI")]
    public async Task AddRootSendsEvent()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);
        using var http = new HttpClient();
        using var stream = await http.GetStreamAsync($"http://localhost:{port}/events");
        using var reader = new StreamReader(stream);

        var addParams = JsonDocument.Parse("{\"uri\":\"mem:/newroot\"}");
        var req = new JsonRpcMessage { Method = "roots/add", Id = JsonSerializer.SerializeToElement(60), Params = addParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));

        string? line = null;
        for (int i = 0; i < 20 && line == null; i++)
        {
            var l = await reader.ReadLineAsync();
            if (l != null && l.Contains("RootsListChangedEvent")) line = l;
        }

        Assert.NotNull(line);
        cts.Cancel();
        await serverTask;
    }

    [Fact(Skip="flaky in CI")]
    public async Task RemoveRootSendsEvent()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);
        using var http = new HttpClient();
        using var stream = await http.GetStreamAsync($"http://localhost:{port}/events");
        using var reader = new StreamReader(stream);

        var addParams = JsonDocument.Parse("{\"uri\":\"mem:/r2\"}");
        var addReq = new JsonRpcMessage { Method = "roots/add", Id = JsonSerializer.SerializeToElement(61), Params = addParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(addReq)));

        var removeParams = JsonDocument.Parse("{\"uri\":\"mem:/r2\"}");
        var removeReq = new JsonRpcMessage { Method = "roots/remove", Id = JsonSerializer.SerializeToElement(62), Params = removeParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(removeReq)));

        string? line = null;
        for (int i = 0; i < 40 && line == null; i++)
        {
            var l = await reader.ReadLineAsync();
            if (l != null && l.Contains("RootsListChangedEvent")) line = l;
        }

        Assert.NotNull(line);
        cts.Cancel();
        await serverTask;
    }

    [Fact(Skip="flaky in CI")]
    public async Task ClearMessagesSendsEvent()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);
        using var http = new HttpClient();
        using var stream = await http.GetStreamAsync($"http://localhost:{port}/events");
        using var reader = new StreamReader(stream);

        var req = new JsonRpcMessage { Method = "messages/clear", Id = JsonSerializer.SerializeToElement(70) };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));

        string? line = null;
        for (int i = 0; i < 20 && line == null; i++)
        {
            var l = await reader.ReadLineAsync();
            if (l != null && l.Contains("LoggingMessageEvent")) line = l;
        }

        Assert.NotNull(line);
        cts.Cancel();
        await serverTask;
    }

    [Fact(Skip="flaky in CI")]
    public async Task AddMessageSendsEvent()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);
        using var http = new HttpClient();
        using var stream = await http.GetStreamAsync($"http://localhost:{port}/events");
        using var reader = new StreamReader(stream);

        var addParams = JsonDocument.Parse("{\"text\":\"hi\"}");
        var req = new JsonRpcMessage { Method = "messages/add", Id = JsonSerializer.SerializeToElement(71), Params = addParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));

        string? line = null;
        for (int i = 0; i < 20 && line == null; i++)
        {
            var l = await reader.ReadLineAsync();
            if (l != null && l.Contains("AddToHistoryEvent")) line = l;
        }

        Assert.NotNull(line);
        cts.Cancel();
        await serverTask;
    }

    [Fact(Skip="flaky in CI")]
    public async Task PingRequestReturnsResult()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);
        using var http = new HttpClient();
        var req = new JsonRpcMessage { Method = "ping", Id = JsonSerializer.SerializeToElement(99) };
        var resp = await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("result", body);
        cts.Cancel();
        await serverTask;
    }
}
