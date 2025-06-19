using CodexCli.Interactive;
using CodexCli.Protocol;
using Xunit;

public class ChatComposerHistoryWidgetIntegrationTests
{
    [Fact]
    public void SubmissionAddsLineToHistory()
    {
        var history = new ConversationHistoryWidget();
        var composer = new ChatComposer(true, new AppEventSender(_ => { }), history);
        composer.HandleKeyEvent(new ConsoleKeyInfo('h', ConsoleKey.H, false, false, false));
        var (res, _) = composer.HandleKeyEvent(new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false));
        Assert.True(res.IsSubmitted);
        var lines = history.GetVisibleLines(1);
        Assert.Contains("[bold cyan]You:[/] h", lines);
    }
}

