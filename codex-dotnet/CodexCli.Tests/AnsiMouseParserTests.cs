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
        string seq = "\u001b[<64;0;0M\u001b[<65;0;0M";
        foreach (var ch in seq)
            Assert.True(parser.ProcessChar(ch));
    }
}

