using System.Collections.Generic;
using CodexCli.Interactive;
using System.Threading;
using System.Threading.Tasks;
using CodexCli.Protocol;
using Xunit;

public class AnsiMouseParserTests
{
    [Fact]
    public async Task ParsesUpAndDown()
    {
        var events = new List<Event>();
        var helper = new ScrollEventHelper(new AppEventSender(ev => events.Add(ev)));
        var parser = new AnsiMouseParser(helper);

        foreach (var ch in "\u001b[<64;0;0M")
            Assert.True(parser.ProcessChar(ch));
        await Task.Delay(150);

        foreach (var ch in "\u001b[<65;0;0M")
            Assert.True(parser.ProcessChar(ch));
        await Task.Delay(150);

        Assert.Equal(2, events.Count);
        Assert.Equal(-1, Assert.IsType<ScrollEvent>(events[0]).Delta);
        Assert.Equal(1, Assert.IsType<ScrollEvent>(events[1]).Delta);
    }
}

