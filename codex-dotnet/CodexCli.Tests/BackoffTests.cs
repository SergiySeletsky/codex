using CodexCli.Util;
using Xunit;

public class BackoffTests
{
    [Fact]
    public void DelayIncreases()
    {
        var d1 = Backoff.GetDelay(1);
        var d2 = Backoff.GetDelay(2);
        Assert.True(d2 > d1);
    }
}
