using CodexCli.Util;
using Xunit;
using System.Threading.Tasks;
using System.IO;

public class ChatGptLoginTests
{
    [Fact]
    public void ScriptEmbedded()
    {
        var path = ChatGptLogin.GetScriptPath();
        Assert.True(File.Exists(path));
    }

}
