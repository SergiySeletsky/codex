using CodexCli.Commands;
using CodexCli.Util;
using System.CommandLine;
using System.CommandLine.Parsing;
using Xunit;

public class LoginCommandTests
{
    [Fact]
    public async Task UsesChatGptLoginWhenFlagProvided()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmp);
        Environment.SetEnvironmentVariable("CODEX_HOME", tmp);
        Environment.SetEnvironmentVariable("HOME", tmp);
        bool called = false;
        LoginCommand.LoginWithChatGptAsync = (home, capture, env) =>
        {
            called = true;
            File.WriteAllText(Path.Combine(tmp, "auth.json"), "{\"OPENAI_API_KEY\":\"k\"}");
            return Task.FromResult("k");
        };
        var root = new RootCommand();
        var cfgOpt = new Option<string?>("--config");
        var cdOpt = new Option<string?>("--cd");
        root.AddOption(cfgOpt);
        root.AddOption(cdOpt);
        root.AddCommand(LoginCommand.Create(cfgOpt, cdOpt));
        var parser = new Parser(root);
        await parser.InvokeAsync("login --chatgpt --token t --provider openai --api-key \"\"");
        Assert.True(called);
        LoginCommand.LoginWithChatGptAsync = ChatGptLogin.LoginAsync;
        Environment.SetEnvironmentVariable("CODEX_HOME", null);
        Environment.SetEnvironmentVariable("HOME", null);
    }
}
