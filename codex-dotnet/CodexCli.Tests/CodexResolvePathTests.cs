using CodexCli.Util;
using System.IO;
using Xunit;

public class CodexResolvePathTests
{
    [Fact]
    public void ReturnsCwdWhenNull()
    {
        var cwd = "/home/user";
        Assert.Equal(cwd, Codex.ResolvePath(cwd, null));
    }

    [Fact]
    public void JoinsRelativePath()
    {
        var cwd = "/home/user";
        var path = "sub/file.txt";
        Assert.Equal(Path.Combine(cwd, path), Codex.ResolvePath(cwd, path));
    }

    [Fact]
    public void ReturnsAbsolutePathUnchanged()
    {
        var cwd = "/home/user";
        var abs = "/etc/passwd";
        Assert.Equal(abs, Codex.ResolvePath(cwd, abs));
    }
}
