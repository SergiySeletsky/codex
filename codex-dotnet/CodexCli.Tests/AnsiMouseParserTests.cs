using System.Collections.Generic;
using CodexCli.Interactive;
using CodexCli.Protocol;
using Xunit;

public class AnsiMouseParserTests
{
    [Fact]
    public void ParsesUpAndDown()
    {
        var events = new List<Event>();
        var helper = new ScrollEventHelper(new AppEventSender(ev => events.Add(ev)));
        var parser = new AnsiMouseParser(helper);
        string seq = "\u001b[<64;0;0M\u001b[<65;0;0M";
        foreach (var ch in seq)
            parser.ProcessChar(ch);

        var deltas = new List<int>();
        foreach (var e in events)
            if (e is ScrollEvent se) deltas.Add(se.Delta);

        Assert.Equal(new[]{-1,1}, deltas.ToArray());
    }
}
