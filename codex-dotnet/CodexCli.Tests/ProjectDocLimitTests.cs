using CodexCli.Config;
using CodexCli.Util;

public class ProjectDocLimitTests
{
    [Fact]
    public void TruncatesProjectDoc()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var docPath = Path.Combine(dir, "AGENTS.md");
        var content = new string('a', 2000);
        File.WriteAllText(docPath, content);
        var cfgPath = Path.Combine(dir, "config.toml");
        File.WriteAllText(cfgPath, "project_doc_max_bytes = 1000");
        var cfg = AppConfig.Load(cfgPath);
        var inst = ProjectDoc.GetUserInstructions(cfg, dir, false, null, null);
        Assert.True(inst!.Length <= 1000);
    }
}
