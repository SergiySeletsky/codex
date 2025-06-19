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
        await Task.Delay(200);
        Assert.InRange(events.Count, 0, 1);
        await Task.Delay(50);
        ConsoleKeyInfo key2 = default;
        for (int i = 0; i < 10 && (!input.TryRead(out key2) || key2.KeyChar == '\0'); i++)
            await Task.Delay(10);
        Assert.Equal('b', key2.KeyChar);
    }


}
