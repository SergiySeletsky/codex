using CodexCli.Commands;
using CodexCli.Config;
using CodexCli.Util;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public class ExecRolloutRecorderTests
{
    [Fact]
    public async Task RecordsRolloutFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        var cfgPath = Path.Combine(dir, "cfg.toml");
        await File.WriteAllTextAsync(cfgPath, $"codex_home = \"{dir.Replace("\\", "/")}\"");
        var content = "event: response.output_item.done\n" +
                      "data: {\"type\":\"response.output_item.done\",\"item\":{\"type\":\"message\",\"role\":\"assistant\",\"content\":[{\"type\":\"text\",\"text\":\"hi\"}]}}\n\n" +
                      "event: response.completed\n" +
                      "data: {\"type\":\"response.completed\",\"response\":{\"id\":\"r1\",\"output\":[]}}\n\n";
        var fixture = Path.GetTempFileName();
        await File.WriteAllTextAsync(fixture, content);
        Environment.SetEnvironmentVariable("CODEX_RS_SSE_FIXTURE", fixture);
        var root = new RootCommand();
        var cfgOpt = new Option<string?>("--config");
        var cdOpt = new Option<string?>("--cd");
        root.AddOption(cfgOpt);
        root.AddOption(cdOpt);
        root.AddCommand(ExecCommand.Create(cfgOpt, cdOpt));
        var parser = new CommandLineBuilder(root).Build();
        await parser.InvokeAsync($"--config {cfgPath} exec hi --model-provider Mock --json");
        Environment.SetEnvironmentVariable("CODEX_RS_SSE_FIXTURE", null);
        File.Delete(fixture);
        var sessionDir = Path.Combine(dir, "sessions");
        var files = Directory.GetFiles(sessionDir, "rollout-*.jsonl");
        Assert.Single(files);
        var lines = File.ReadAllLines(files[0]);
        Assert.True(lines.Length >= 2);
    }
}
