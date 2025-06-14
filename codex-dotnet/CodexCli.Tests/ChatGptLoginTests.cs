using CodexCli.Util;
using Xunit;

public class ChatGptLoginTests
{
    [Fact]
    public void ScriptEmbedded()
    {
        var path = ChatGptLogin.GetScriptPath();
        Assert.True(File.Exists(path));
    }
}
