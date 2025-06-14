namespace CodexCli.Tests;

using CodexCli.Config;
using CodexCli.Util;

public class ProjectDocTests
{
    [Fact]
    public void GetUserInstructions_MergesDocs()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "AGENTS.md"), "proj");
        var cfgPath = Path.Combine(dir, "config.toml");
        File.WriteAllText(cfgPath, "instructions='base'");
        var cfg = AppConfig.Load(cfgPath);
        var inst = ProjectDoc.GetUserInstructions(cfg, dir);
        Assert.Contains("base", inst);
        Assert.Contains("proj", inst);
    }

    [Fact]
    public void GetUserInstructions_DisabledViaEnv()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "AGENTS.md"), "proj");
        var cfgPath = Path.Combine(dir, "config.toml");
        File.WriteAllText(cfgPath, "instructions='base'");
        var cfg = AppConfig.Load(cfgPath);
        Environment.SetEnvironmentVariable("CODEX_DISABLE_PROJECT_DOC", "1");
        var inst = ProjectDoc.GetUserInstructions(cfg, dir);
        Environment.SetEnvironmentVariable("CODEX_DISABLE_PROJECT_DOC", null);
        Assert.Equal("base", inst);
    }
}
