using CodexCli.Commands;
using CodexCli.Config;
using System.CommandLine;
using System.CommandLine.Parsing;
using Xunit;

public class ProviderCommandTests
{
    [Fact]
    public void ListShowsBuiltIns()
    {
        var root = new RootCommand();
        var cfgOpt = new Option<string?>("--config");
        root.AddOption(cfgOpt);
        root.AddCommand(ProviderCommand.Create(cfgOpt));
        var parser = new Parser(root);
        var output = new StringWriter();
        Console.SetOut(output);
        parser.Invoke("provider list");
        var text = output.ToString();
        Assert.Contains("openai", text);
        Assert.Contains("openrouter", text);
    }
}
