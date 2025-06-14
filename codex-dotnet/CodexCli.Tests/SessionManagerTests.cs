using System.IO;

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
        var file = CodexCli.Util.SessionManager.GetHistoryFile(id);
        Assert.NotNull(file);
        Assert.True(File.Exists(file));
    }

    [Fact]
    public void ClearsHistory()
    {
        var id = CodexCli.Util.SessionManager.CreateSession();
        CodexCli.Util.SessionManager.AddEntry(id, "hello");
        CodexCli.Util.SessionManager.ClearHistory(id);
        var hist = CodexCli.Util.SessionManager.GetHistory(id);
        Assert.Empty(hist);
    }
}
