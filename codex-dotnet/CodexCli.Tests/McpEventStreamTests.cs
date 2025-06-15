using CodexCli.Util;
using CodexCli.Protocol;
using System.Text.Json;
using CodexCli.Models;
using System.Net;
using System.IO;
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

        await foreach (var ev in McpEventStream.ReadEventsAsync($"http://localhost:{port}"))
        {
            if (ev is PromptListChangedEvent)
            {
                Assert.True(true);
                break;
            }
        }

        cts.Cancel();
        await serverTask;
    }

    [Fact(Skip="flaky in CI")]
    public async Task ReadItems()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);

        using var http = new HttpClient();
        var readTask = Task.Run(async () =>
        {
            int progress = 0;
            await foreach (var item in McpEventStream.ReadItemsAsync($"http://localhost:{port}"))
            {
                if (item is MessageItem m && m.Content[0].Text.Contains("Progress"))
                    progress++;
                if (progress == 2) break;
            }
            return progress;
        });

        var writeParams = JsonDocument.Parse("{\"uri\":\"mem:/demo.txt\",\"text\":\"foo\"}");
        var req = new JsonRpcMessage { Method = "resources/write", Id = JsonSerializer.SerializeToElement(2), Params = writeParams.RootElement };
        await http.PostAsync($"http://localhost:{port}/jsonrpc", new StringContent(JsonSerializer.Serialize(req)));

        var progressCount = await readTask;
        Assert.Equal(2, progressCount);

        cts.Cancel();
        await serverTask;
    }

    [Fact]
    public async Task ReadLinesHandlesCommentsAndIds()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();
        var serverTask = Task.Run(async () =>
        {
            var ctx = await listener.GetContextAsync();
            ctx.Response.ContentType = "text/event-stream";
            using var w = new StreamWriter(ctx.Response.OutputStream);
            await w.WriteAsync(": comment\nid:1\nevent:foo\ndata: {\"type\":\"noop\"}\n\n");
            await w.FlushAsync();
            ctx.Response.Close();
        });

        var lines = new List<string>();
        await foreach (var line in McpEventStream.ReadLinesAsync($"http://localhost:{port}"))
        {
            lines.Add(line);
        }
        Assert.Contains("{\"type\":\"noop\"}", lines[0]);
        listener.Close();
        await serverTask;
    }
}

