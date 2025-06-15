using System.Threading.Channels;

namespace CodexCli.Models;

public abstract record ResponseEvent;

public record OutputItemDone(ResponseItem Item) : ResponseEvent;
public record Completed(string ResponseId) : ResponseEvent;

public class ResponseStream : IAsyncEnumerable<ResponseEvent>
{
    private readonly Channel<ResponseEvent> _channel = Channel.CreateUnbounded<ResponseEvent>();

    public ChannelWriter<ResponseEvent> Writer => _channel.Writer;

    public IAsyncEnumerator<ResponseEvent> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator();
}
