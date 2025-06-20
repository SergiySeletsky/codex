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

    [Fact]
    public async Task ParsesBracketedPaste()
    {
        using var reader = new StringReader("\u001b[200~a\nb\u001b[201~");
        var helper = new ScrollEventHelper(new AppEventSender(_ => { }));
        var parser = new AnsiMouseParser(helper);
        using var input = new PtyInputReader(reader, parser);

        await Task.Delay(100);
        var keys = new List<ConsoleKeyInfo>();
        for (int i = 0; i < 3; i++)
        {
            Assert.True(input.TryRead(out var k));
            keys.Add(k);
        }
        Assert.Equal('a', keys[0].KeyChar);
        Assert.Equal('\n', keys[1].KeyChar);
        Assert.Equal(ConsoleModifiers.Shift, keys[1].Modifiers);
        Assert.Equal('b', keys[2].KeyChar);
    }

    [Fact]
    public async Task HandlesInvalidPasteSequence()
    {
        using var reader = new StringReader("\u001b[200Xab");
        var helper = new ScrollEventHelper(new AppEventSender(_ => { }));
        var parser = new AnsiMouseParser(helper);
        using var input = new PtyInputReader(reader, parser);

        await Task.Delay(100);
        Assert.False(input.TryRead(out _));
    }

    [Fact]
    public async Task PasteBufferIsCapped()
    {
        var longText = new string('x', PtyInputReader.MaxPasteLength + 50);
        using var reader = new StringReader($"\u001b[200~{longText}\u001b[201~");
        var helper = new ScrollEventHelper(new AppEventSender(_ => { }));
        var parser = new AnsiMouseParser(helper);
        using var input = new PtyInputReader(reader, parser);

        await Task.Delay(200);
        int count = 0;
        while (input.TryRead(out _)) count++;
        Assert.True(count >= PtyInputReader.MaxPasteLength);
    }
}
