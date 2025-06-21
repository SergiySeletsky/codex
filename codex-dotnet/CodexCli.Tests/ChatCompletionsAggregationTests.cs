using CodexCli.Models;
using CodexCli.Util;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class ChatCompletionsAggregationTests
{
    private async IAsyncEnumerable<ResponseEvent> SampleEvents()
    {
        yield return new OutputItemDone(new MessageItem("assistant", new List<ContentItem>{ new("output_text", "he") }));
        yield return new OutputItemDone(new MessageItem("assistant", new List<ContentItem>{ new("output_text", "llo") }));
        yield return new Completed("1");
    }

    [Fact]
    public async Task AggregatesAssistantChunks()
    {
        List<ResponseEvent> list = new();
        await foreach (var ev in SampleEvents().Aggregate())
            list.Add(ev);
        Assert.Equal(2, list.Count);
        var msg = Assert.IsType<OutputItemDone>(list[0]);
        Assert.Equal("hello", ((MessageItem)msg.Item).Content[0].Text);
        Assert.IsType<Completed>(list[1]);
    }
}
