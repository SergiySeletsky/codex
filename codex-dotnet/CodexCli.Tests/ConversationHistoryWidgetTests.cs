using CodexCli.Interactive;
using Xunit;

public class ConversationHistoryWidgetTests
{
    [Fact]
    public void ScrollHistory()
    {
        var hist = new ConversationHistoryWidget();
        for (int i = 0; i < 5; i++)
            hist.Add($"line {i}");
        var lines = hist.GetVisibleLines(3);
        Assert.Equal(new[]{"line 2","line 3","line 4"}, lines);

        hist.ScrollUp(1);
        lines = hist.GetVisibleLines(3);
        Assert.Equal(new[]{"line 1","line 2","line 3"}, lines);

        hist.ScrollDown(1);
        lines = hist.GetVisibleLines(3);
        Assert.Equal(new[]{"line 2","line 3","line 4"}, lines);

        hist.ScrollPageUp(2);
        lines = hist.GetVisibleLines(3);
        Assert.Equal(new[]{"line 0","line 1","line 2"}, lines);

        hist.ScrollPageDown(2);
        lines = hist.GetVisibleLines(3);
        Assert.Equal(new[]{"line 2","line 3","line 4"}, lines);

        hist.ScrollToBottom();
        lines = hist.GetVisibleLines(3);
        Assert.Equal(new[]{"line 2","line 3","line 4"}, lines);
    }
}
