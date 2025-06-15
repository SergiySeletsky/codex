using CodexCli.Commands;
using CodexCli.Models;
using CodexCli.Util;
using System.Collections.Generic;
using System.IO;
using System.CommandLine.Builder;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using Xunit;

public class ReplayCommandTests
{
    [Fact]
    public async Task PrintsMessages()
    {
        var items = new ResponseItem[]
        {
            new MessageItem("assistant", new List<ContentItem>{ new("output_text","hi") }),
            new FunctionCallItem("tool","{}","1")
        };
        var file = Path.GetTempFileName();
        await using (var w = new StreamWriter(file))
        {
            foreach (var i in items)
                await w.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(i, i.GetType()));
        }
        var cmd = ReplayCommand.Create();
        var console = new System.CommandLine.IO.TestConsole();
        var parser = new CommandLineBuilder(cmd).Build();
        await parser.InvokeAsync(new[] { file }, console);
        Assert.Contains("assistant: hi", console.Out.ToString());
        File.Delete(file);
    }

    [Fact]
    public async Task JsonOutput()
    {
        var items = new ResponseItem[]
        {
            new MessageItem("assistant", new List<ContentItem>{ new("output_text","hi") })
        };
        var file = Path.GetTempFileName();
        await using (var w = new StreamWriter(file))
        {
            foreach (var i in items)
                await w.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(i, i.GetType()));
        }
        var cmd = ReplayCommand.Create();
        var console = new TestConsole();
        var parser = new CommandLineBuilder(cmd).Build();
        await parser.InvokeAsync(new[] { "--json", file }, console);
        Assert.Contains("\"assistant\"", console.Out.ToString());
        File.Delete(file);
    }
}
