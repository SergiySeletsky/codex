using CodexCli.Protocol;
using CodexCli.Util;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;

public class CodexSendEventTests
{
    [Fact]
    public async Task SendEvent_WritesToChannel()
    {
        var ch = Channel.CreateUnbounded<Event>();
        await Codex.SendEventAsync(ch.Writer, new ErrorEvent("1", "oops"));
        var evt = await ch.Reader.ReadAsync();
        Assert.IsType<ErrorEvent>(evt);
    }

    [Fact]
    public async Task SendEvent_ClosedChannelDoesNotThrow()
    {
        var ch = Channel.CreateUnbounded<Event>();
        ch.Writer.Complete();
        await Codex.SendEventAsync(ch.Writer, new ErrorEvent("1", "oops"));
    }
}
