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
    [Fact]
    public void LoadDefault_UsesEnvVar()
    {
        var file = Path.GetTempFileName();
        File.WriteAllText(file, "define_program( program=\"foo\" )");
        Environment.SetEnvironmentVariable("CODEX_EXEC_POLICY_PATH", file);
        var policy = ExecPolicy.LoadDefault();
        Environment.SetEnvironmentVariable("CODEX_EXEC_POLICY_PATH", null);
        Assert.True(policy.IsAllowed("foo"));
    }
}
