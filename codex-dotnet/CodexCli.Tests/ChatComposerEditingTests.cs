using CodexCli.Interactive;
using Xunit;

public class ChatComposerEditingTests
{
    [Fact]
    public void CursorMovementAndEdit()
    {
        var composer = new ChatComposer(true, new AppEventSender(_ => { }));
        composer.HandleKeyEvent(new ConsoleKeyInfo('h', ConsoleKey.H, false, false, false));
        composer.HandleKeyEvent(new ConsoleKeyInfo('i', ConsoleKey.I, false, false, false));
        composer.HandleKeyEvent(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));
        composer.HandleKeyEvent(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
        var (res, _) = composer.HandleKeyEvent(new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false));
        Assert.True(res.IsSubmitted);
        Assert.Equal("hai", res.SubmittedText);
    }

    [Fact]
    public void BackspaceDeletesBeforeCursor()
    {
        var composer = new ChatComposer(true, new AppEventSender(_ => { }));
        composer.HandleKeyEvent(new ConsoleKeyInfo('h', ConsoleKey.H, false, false, false));
        composer.HandleKeyEvent(new ConsoleKeyInfo('i', ConsoleKey.I, false, false, false));
        composer.HandleKeyEvent(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));
        composer.HandleKeyEvent(new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false));
        var (res, _) = composer.HandleKeyEvent(new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, false));
        Assert.True(res.IsSubmitted);
        Assert.Equal("i", res.SubmittedText);
    }
}
