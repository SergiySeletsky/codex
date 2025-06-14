namespace CodexCli.Tests;

using CodexCli.Config;
using System.IO;

public class AppConfigExtraTests
{
    [Fact]
    public void ParsesHistoryAndFileOpener()
    {
        var toml = "file_opener = 'vscode'\n" +
                    "[history]\n" +
                    "persistence = 'none'\n" +
                    "max_bytes = 1024\n" +
                    "[tui]\n" +
                    "disable_mouse_capture = true\n";
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, toml);

        var cfg = AppConfig.Load(tmp);
        Assert.Equal(HistoryPersistence.None, cfg.History.Persistence);
        Assert.Equal(1024, cfg.History.MaxBytes);
        Assert.True(cfg.Tui.DisableMouseCapture);
    }
}
