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
        var vars = new Dictionary<string,string>
        {
            {"PATH","/usr/bin"},
            {"HOME","/home/user"},
            {"API_KEY","secret"},
            {"SECRET_TOKEN","t"}
        };
        var env = ExecEnv.CreateFrom(vars, policy);
        Assert.DoesNotContain(env.Keys, k => k.Contains("KEY") || k.Contains("TOKEN"));
        Assert.Equal(2, env.Count);
        Assert.Contains("PATH", env.Keys);
        Assert.Contains("HOME", env.Keys);
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
        var vars = new Dictionary<string,string>
        {
            {"PATH","/usr/bin"},
            {"FOO","bar"}
        };
        var env = ExecEnv.CreateFrom(vars, policy);
        Assert.True(env.Keys.All(k => k.Equals("PATH", System.StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void SetOverrides()
    {
        var vars = new Dictionary<string,string> { {"PATH","/usr/bin"} };
        var policy = new ShellEnvironmentPolicy { IgnoreDefaultExcludes = true };
        policy.Set["NEW_VAR"] = "42";
        var env = ExecEnv.CreateFrom(vars, policy);
        Assert.Equal("42", env["NEW_VAR"]);
        Assert.Equal("/usr/bin", env["PATH"]);
    }

    [Fact]
    public void InheritAll()
    {
        var vars = new Dictionary<string,string> { {"PATH","/usr/bin"}, {"FOO","bar"} };
        var policy = new ShellEnvironmentPolicy
        {
            Inherit = ShellEnvironmentPolicyInherit.All,
            IgnoreDefaultExcludes = true
        };
        var env = ExecEnv.CreateFrom(vars, policy);
        Assert.Equal(2, env.Count);
        Assert.Equal("bar", env["FOO"]);
    }

    [Fact]
    public void InheritAllWithDefaultExcludes()
    {
        var vars = new Dictionary<string,string> { {"PATH","/usr/bin"}, {"API_KEY","secret"} };
        var policy = new ShellEnvironmentPolicy { Inherit = ShellEnvironmentPolicyInherit.All };
        var env = ExecEnv.CreateFrom(vars, policy);
        Assert.Equal(new[]{"PATH"}, env.Keys.ToArray());
    }

    [Fact]
    public void InheritNone()
    {
        var vars = new Dictionary<string,string> { {"PATH","/usr/bin"}, {"HOME","/home"} };
        var policy = new ShellEnvironmentPolicy
        {
            Inherit = ShellEnvironmentPolicyInherit.None,
            IgnoreDefaultExcludes = true
        };
        policy.Set["ONLY_VAR"] = "yes";
        var env = ExecEnv.CreateFrom(vars, policy);
        Assert.Equal(new[]{"ONLY_VAR"}, env.Keys.ToArray());
        Assert.Equal("yes", env["ONLY_VAR"]);
    }
}
