using CodexCli.Protocol;
using Xunit;

public class SandboxPolicyTests
{
    [Fact]
    public void WritableRootsIncludeCwdAndFolders()
    {
        var policy = SandboxPolicy.NewReadOnlyPolicyWithWritableRoots(new[]{"/tmp"});
        var roots = policy.GetWritableRootsWithCwd("/home/test");
        Assert.Contains("/home/test", roots);
        Assert.Contains("/tmp", roots);
    }
}
