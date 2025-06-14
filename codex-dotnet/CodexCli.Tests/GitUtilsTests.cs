using CodexCli.Util;
using Xunit;

public class GitUtilsTests
{
    [Fact]
    public void DetectsGitRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        var root = GitUtils.GetRepoRoot(dir);
        Assert.NotNull(root);
        Assert.True(Directory.Exists(Path.Combine(root!, ".git")));
    }
}
