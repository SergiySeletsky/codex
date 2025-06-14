using CodexCli.Commands;
using CodexCli.Config;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
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

    [Fact]
    public void AddRemoveProvider()
    {
        var tmp = Path.GetTempFileName();
        try
        {
        var root = new RootCommand();
        var cfgOpt = new Option<string?>("--config");
        root.AddOption(cfgOpt);
        root.AddCommand(ProviderCommand.Create(cfgOpt));
        var parser = new Parser(root);
        parser.Invoke($"provider add newprov --name X --base-url https://x.com --config {tmp}");
        var cfg = AppConfig.Load(tmp);
        Assert.NotNull(cfg.ModelProviders);
        parser.Invoke($"provider remove newprov --config {tmp}");
        cfg = AppConfig.Load(tmp);
        Assert.NotNull(cfg.ModelProviders);
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void SetDefaultProvider()
    {
        var tmp = Path.GetTempFileName();
        try
        {
        var root = new RootCommand();
        var cfgOpt = new Option<string?>("--config");
        root.AddOption(cfgOpt);
        root.AddCommand(ProviderCommand.Create(cfgOpt));
        var parser = new Parser(root);
        parser.Invoke($"provider set-default openai --config {tmp}");
        var cfg = AppConfig.Load(tmp);
        Assert.NotNull(cfg);
        }
        finally { File.Delete(tmp); }
    }
}
