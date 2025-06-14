using CodexCli.Util;
using Xunit;

public class ExecPolicyTests
{
    [Fact]
    public void AllowsKnownProgram()
    {
        var policy = ExecPolicy.LoadDefault();
        Assert.True(policy.IsAllowed("ls"));
    }

    [Fact]
    public void DeniesUnknownProgram()
    {
        var policy = ExecPolicy.LoadDefault();
        Assert.False(policy.IsAllowed("rm"));
    }
}
