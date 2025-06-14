using CodexCli.Util;

public class ElapsedTests
{
    [Fact]
    public void FormatsSubSecond()
    {
        var ts = TimeSpan.FromMilliseconds(250);
        Assert.Equal("250ms", Elapsed.FormatDuration(ts));
    }

    [Fact]
    public void FormatsSeconds()
    {
        var ts = TimeSpan.FromMilliseconds(1500);
        Assert.Equal("1.50s", Elapsed.FormatDuration(ts));
    }

    [Fact]
    public void FormatsMinutes()
    {
        var ts = TimeSpan.FromMilliseconds(75000);
        Assert.Equal("1m15s", Elapsed.FormatDuration(ts));
    }
}

