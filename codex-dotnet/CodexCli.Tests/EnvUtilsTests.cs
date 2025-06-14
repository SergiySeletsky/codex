namespace CodexCli.Tests;

public class EnvUtilsTests
{
    [Fact]
    public void UsesEnvVarIfSet()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "codexhome-test");
        Directory.CreateDirectory(tmp);
        Environment.SetEnvironmentVariable("CODEX_HOME", tmp);
        var res = CodexCli.Util.EnvUtils.FindCodexHome();
        Assert.Equal(Path.GetFullPath(tmp), res);
        Environment.SetEnvironmentVariable("CODEX_HOME", null);
        Directory.Delete(tmp, true);
    }
}
