using CodexCli.Interactive;
using CodexCli.Protocol;
using System.Collections.Generic;
using Xunit;

public class ChatComposerHistoryTests
{
    private class MockTextArea : ITextArea
    {
        private List<string> _lines = new() { "" };
        public IReadOnlyList<string> Lines => _lines;
        public (int Row, int Col) Cursor { get; private set; } = (0,0);
        public void SelectAll() { }
        public void Cut() { _lines = new() { "" }; }
        public void InsertString(string text) => _lines = new(text.Split('\n'));
        public void MoveCursor(int row, int col) { Cursor = (row,col); }
    }

    [Fact]
    public void NavigationWithAsyncFetch()
    {
        var events = new List<Event>();
        var sender = new AppEventSender(ev => events.Add(ev));
        var hist = new ChatComposerHistory();
        hist.SetMetadata("1", 3);
        var ta = new MockTextArea();

        Assert.True(hist.ShouldHandleNavigation(ta));
        Assert.True(hist.NavigateUp(ta, sender));
        var req = Assert.IsType<GetHistoryEntryRequestEvent>(Assert.Single(events));
        Assert.Equal("1", req.SessionId);
        Assert.Equal(2, req.Offset);
        Assert.Equal("", string.Join("\n", ta.Lines));

        Assert.True(hist.OnEntryResponse("1", 2, "latest", ta));
        Assert.Equal("latest", string.Join("\n", ta.Lines));

        events.Clear();
        Assert.True(hist.NavigateUp(ta, sender));
        req = Assert.IsType<GetHistoryEntryRequestEvent>(Assert.Single(events));
        Assert.Equal(1, req.Offset);
        hist.OnEntryResponse("1", 1, "older", ta);
        Assert.Equal("older", string.Join("\n", ta.Lines));
    }
}
