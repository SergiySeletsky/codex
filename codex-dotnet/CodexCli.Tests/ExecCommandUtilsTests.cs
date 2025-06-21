using CodexCli.Util;
using Xunit;

public class ExecCommandUtilsTests
{
    [Fact]
    public void EscapeCommandQuotesArgs()
    {
        var cmd = ExecCommandUtils.EscapeCommand(new[]{"foo","bar baz","weird&stuff"});
        Assert.Equal("foo 'bar baz' 'weird&stuff'", cmd);
    }

    [Fact]
    public void StripBashLcReturnsInner()
    {
        var cmd = ExecCommandUtils.StripBashLcAndEscape(new[]{"bash","-lc","echo hello"});
        Assert.Equal("echo hello", cmd);
    }

    [Fact]
    public void RelativizeToHomeReturnsRelative()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var path = Path.Combine(home, "dir", "file.txt");
        var rel = ExecCommandUtils.RelativizeToHome(path);
        Assert.Equal($"dir{Path.DirectorySeparatorChar}file.txt", rel);
    }
}
