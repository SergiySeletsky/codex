using CodexTui;
using CodexCli.Interactive;
using CodexCli.Protocol;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public class PtyInputReaderTests
{
    [Fact]
    public async Task ReadsCharsAndIgnoresMouseSequences()
    {
        var events = new List<Event>();
        var helper = new ScrollEventHelper(new AppEventSender(e => events.Add(e)));
        var parser = new AnsiMouseParser(helper);
        using var reader = new StringReader("a\u001b[<64;0;0Mb");
        using var input = new PtyInputReader(reader, parser);

        await Task.Delay(100);
        Assert.True(input.TryRead(out var key1));
        Assert.Equal('a', key1.KeyChar);
        await Task.Delay(150);
        Assert.Single(events);
        await Task.Delay(50);
        Assert.True(input.TryRead(out var key2));
        Assert.Equal('b', key2.KeyChar);
    }

}
