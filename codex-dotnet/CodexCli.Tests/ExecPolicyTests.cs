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
        Assert.True(policy.IsForbidden("rm"));
        Assert.Equal("dangerous", policy.GetReason("rm"));
    }

    [Fact]
    public void VerifyCommand_AllowsExpectedFlags()
    {
        var policy = ExecPolicy.LoadDefault();
        Assert.True(policy.VerifyCommand("ls", new[] { "-l" }));
    }

    [Fact]
    public void VerifyCommand_RejectsUnknownFlag()
    {
        var policy = ExecPolicy.LoadDefault();
        Assert.False(policy.VerifyCommand("ls", new[] { "--foo" }));
    }
}
