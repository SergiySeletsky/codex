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

    [Fact]
    public void HistoryDirPrefersEnv()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "codexhistory-test");
        Directory.CreateDirectory(tmp);
        Environment.SetEnvironmentVariable("CODEX_HISTORY_DIR", tmp);
        var res = CodexCli.Util.EnvUtils.GetHistoryDir();
        Assert.Equal(tmp, res);
        Environment.SetEnvironmentVariable("CODEX_HISTORY_DIR", null);
        Directory.Delete(tmp, true);
    }

    [Fact]
    public void HistoryDirFallsBackToHome()
    {
        Environment.SetEnvironmentVariable("CODEX_HISTORY_DIR", null);
        var home = Path.Combine(Path.GetTempPath(), "codexhome-default");
        Directory.CreateDirectory(home);
        Environment.SetEnvironmentVariable("CODEX_HOME", home);
        var res = CodexCli.Util.EnvUtils.GetHistoryDir();
        Assert.Equal(Path.Combine(home, "history"), res);
        Environment.SetEnvironmentVariable("CODEX_HOME", null);
        Directory.Delete(home, true);
    }
}
