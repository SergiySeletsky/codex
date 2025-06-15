using CodexCli.Util;
using CodexCli.Protocol;
using System.Text.Json;
using Xunit;

public class McpEventStreamTests
{
    [Fact(Skip="flaky in CI")]
    public async Task ReadEvents()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);

        using var http = new HttpClient();
        var addParams = JsonDocument.Parse("{\"name\":\"test\",\"message\":\"hi\"}");
        var req = new JsonRpcMessage { Method = "prompts/add", Id = JsonSerializer.SerializeToElement(1), Params = addParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));

        await foreach (var line in McpEventStream.ReadLinesAsync($"http://localhost:{port}"))
        {
            Assert.Contains("PromptListChangedEvent", line);
            break;
        }

        cts.Cancel();
        await serverTask;
    }
}

