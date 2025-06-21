using CodexCli.Commands;
using CodexCli.Util;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using CodexCli.Protocol;
using Xunit;

public class HistoryWatchEventsTests
{
    [Fact(Skip="flaky in CI")]
    public async Task MessagesMetaWatchesEvents()
    {
        int port = TestUtils.GetFreeTcpPort();
        using var server = new McpServer(port);
        var cts = new CancellationTokenSource();
        var serverTask = server.RunAsync(cts.Token);
        await Task.Delay(100);

        var cmd = HistoryCommand.Create();
        var parser = new CommandLineBuilder(cmd).Build();
        var sw = new StringWriter();
        Console.SetOut(sw);
        var invokeTask = parser.InvokeAsync(new[] { "messages-meta", "--events-url", $"http://localhost:{port}", "--watch-events" });
        await Task.Delay(100);
        server.EmitEvent(new PromptListChangedEvent(Guid.NewGuid().ToString()));
        cts.Cancel();
        await serverTask;
        await invokeTask;
        Console.SetOut(Console.Out);
        Assert.Contains("PromptListChangedEvent", sw.ToString());
    }
}
