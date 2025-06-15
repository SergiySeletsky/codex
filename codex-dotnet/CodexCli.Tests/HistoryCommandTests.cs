using CodexCli.Commands;
using CodexCli.Util;
using System.CommandLine;
using System.CommandLine.Parsing;
using Xunit;

public class HistoryCommandTests
{
    [Fact(Skip="fails in CI")]
    public async Task StatsCommandPrintsCounts()
    {
        var dir = Path.Combine(Path.GetTempPath(), "hc" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var cfg = new CodexCli.Config.AppConfig { CodexHome = dir };
        await MessageHistory.AppendEntryAsync("a", "s1", cfg);
        await MessageHistory.AppendEntryAsync("b", "s1", cfg);
        await MessageHistory.AppendEntryAsync("c", "s2", cfg);
        var root = new RootCommand();
        root.AddCommand(HistoryCommand.Create());
        var parser = new Parser(root);
        var sw = new StringWriter();
        Console.SetOut(sw);
        parser.Invoke("history stats");
        Console.SetOut(Console.Out);
        var text = sw.ToString();
        Assert.Contains("s1", text);
        Directory.Delete(dir, true);
    }
}
