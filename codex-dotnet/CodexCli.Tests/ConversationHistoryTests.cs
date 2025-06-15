using CodexCli.Util;
using CodexCli.Models;

public class ConversationHistoryTests
{
    [Fact]
    public void RecordAndRetrieve()
    {
        var hist = new ConversationHistory();
        hist.RecordItems(new ResponseItem[]{ new MessageItem("user", new List<ContentItem>{ new("input_text","hi") }), new MessageItem("system", new List<ContentItem>{ new("input_text","secret") }) });
        Assert.Single(hist.Contents());
    }
}
