using CodexCli.Config;
using CodexCli.Util;
using Xunit;
using System.Collections.Generic;
using System.Linq;

public class ExecEnvTests
{
    [Fact]
    public void CoreInheritAndDefaultExcludes()
    {
        var policy = new ShellEnvironmentPolicy();
        var env = ExecEnv.Create(policy);
        Assert.DoesNotContain(env.Keys, k => k.Contains("KEY") || k.Contains("TOKEN"));
    }

    [Fact]
    public void IncludeOnlyWhitelist()
    {
        var policy = new ShellEnvironmentPolicy
        {
            IgnoreDefaultExcludes = true,
            IncludeOnly = new List<EnvironmentVariablePattern>
            {
                EnvironmentVariablePattern.CaseInsensitive("PATH")
            }
        };
        var env = ExecEnv.Create(policy);
        Assert.True(env.Keys.All(k => k.Equals("PATH", System.StringComparison.OrdinalIgnoreCase)));
    }
}
