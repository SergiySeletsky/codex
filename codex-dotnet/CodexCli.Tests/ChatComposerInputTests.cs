using CodexCli.Interactive;
using Xunit;

public class ChatComposerInputTests
{
    [Fact]
    public void TabCompletesCommand()
    {
        var composer = new ChatComposer(true, new AppEventSender(_ => { }));
        composer.HandleKeyEvent(new ConsoleKeyInfo('/', ConsoleKey.Oem2, false, false, false));
        composer.HandleKeyEvent(new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false));
        var (res1, _) = composer.HandleKeyEvent(new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false));
        Assert.False(res1.IsSubmitted);
        Assert.True(composer.IsCommandPopupVisible);
        var (res2, _) = composer.HandleKeyEvent(new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false));
        Assert.True(res2.IsSubmitted);
        Assert.Equal("/quit", res2.SubmittedText);
    }
}
