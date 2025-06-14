namespace CodexCli.Tests;

public class SessionManagerTests
{
    [Fact]
    public void CreatesAndStoresHistory()
    {
        var id = CodexCli.Util.SessionManager.CreateSession();
        CodexCli.Util.SessionManager.AddEntry(id, "hello");
        var hist = CodexCli.Util.SessionManager.GetHistory(id);
        Assert.Single(hist);
        Assert.Equal("hello", hist[0]);
    }
}
