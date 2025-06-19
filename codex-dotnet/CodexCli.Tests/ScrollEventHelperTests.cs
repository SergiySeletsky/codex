using System.Collections.Generic;
using System.Threading.Tasks;
using CodexCli.Interactive;
using CodexCli.Protocol;
using Xunit;

public class ScrollEventHelperTests
{
    [Fact]
    public async Task DebouncesAccumulatedScroll()
    {
        var events = new List<Event>();
        var helper = new ScrollEventHelper(new AppEventSender(ev => events.Add(ev)));
        helper.ScrollUp();
        helper.ScrollUp();
        await Task.Delay(150);
        Assert.Single(events);
        var se = Assert.IsType<ScrollEvent>(events[0]);
        Assert.Equal(-2, se.Delta);
    }
}

