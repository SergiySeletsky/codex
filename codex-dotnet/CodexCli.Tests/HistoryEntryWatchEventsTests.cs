using CodexCli.Commands;
using CodexCli.Util;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Xunit;

public class HistoryEntryWatchEventsTests
{
    [Fact(Skip="flaky in CI")]
    public async Task MessagesEntryWatchesEvents()
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
        var invokeTask = parser.InvokeAsync(new[] { "messages-entry", "0", "--events-url", $"http://localhost:{port}", "--watch-events" });
        await Task.Delay(100);
        server.EmitEvent(new PromptListChangedEvent(Guid.NewGuid().ToString()));
        cts.Cancel();
        await serverTask;
        await invokeTask;
        Console.SetOut(Console.Out);
        Assert.Contains("PromptListChangedEvent", sw.ToString());
    }
}
