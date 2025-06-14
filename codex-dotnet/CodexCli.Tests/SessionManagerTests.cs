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

    [Fact]
    public void ListsAndDeletesSessions()
    {
        var id = CodexCli.Util.SessionManager.CreateSession();
        CodexCli.Util.SessionManager.AddEntry(id, "x");
        Assert.Contains(id, CodexCli.Util.SessionManager.ListSessions());
        Assert.NotNull(CodexCli.Util.SessionManager.GetStartTime(id));
        Assert.True(CodexCli.Util.SessionManager.DeleteSession(id));
        Assert.DoesNotContain(id, CodexCli.Util.SessionManager.ListSessions());
    }

    [Fact]
    public void PurgesSessions()
    {
        var id1 = CodexCli.Util.SessionManager.CreateSession();
        var id2 = CodexCli.Util.SessionManager.CreateSession();
        CodexCli.Util.SessionManager.AddEntry(id1, "a");
        CodexCli.Util.SessionManager.AddEntry(id2, "b");
        CodexCli.Util.SessionManager.DeleteAllSessions();
        Assert.Empty(CodexCli.Util.SessionManager.ListSessions());
    }

    [Fact]
    public void ListsSessionsWithInfo()
    {
        var id = CodexCli.Util.SessionManager.CreateSession();
        var list = CodexCli.Util.SessionManager.ListSessionsWithInfo().ToList();
        Assert.Contains(list, i => i.Id == id);
        CodexCli.Util.SessionManager.DeleteSession(id);
    }
}
