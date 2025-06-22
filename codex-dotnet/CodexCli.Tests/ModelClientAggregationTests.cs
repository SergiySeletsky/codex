using CodexCli.Models;
using CodexCli.Util;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class ModelClientAggregationTests
{
    [Fact]
    public async Task ResponseStreamAggregateAggregates()
    {
        var src = new ResponseStream();
        _ = Task.Run(async () =>
        {
            src.Writer.TryWrite(new OutputItemDone(new MessageItem("assistant", new List<ContentItem>{ new("output_text", "he") })));        
            await Task.Delay(1);
            src.Writer.TryWrite(new OutputItemDone(new MessageItem("assistant", new List<ContentItem>{ new("output_text", "llo") })));        
            await Task.Delay(1);
            src.Writer.TryWrite(new Completed("x"));
            src.Writer.Complete();
        });
        var list = new List<ResponseEvent>();
        await foreach (var ev in src.Aggregate())
            list.Add(ev);
        Assert.Equal(2, list.Count);
        Assert.Equal("hello", ((MessageItem)((OutputItemDone)list[0]).Item).Content[0].Text);
        Assert.IsType<Completed>(list[1]);
    }
}
