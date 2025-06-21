using CodexCli.Util;
using ModelsPrompt = CodexCli.Models.Prompt;
using System.Text.Json;
using Xunit;

public class OpenAiToolsTests
{
    [Fact]
    public void ResponsesApi_DefaultModelIncludesShell()
    {
        var prompt = new ModelsPrompt();
        var list = OpenAiTools.CreateToolsJsonForResponsesApi(prompt, "gpt-4");
        Assert.Single(list);
        var tool = list[0];
        Assert.Equal("function", tool.GetProperty("type").GetString());
        Assert.Equal("shell", tool.GetProperty("function").GetProperty("name").GetString());
    }

    [Fact]
    public void ResponsesApi_CodexModelIncludesLocalShell()
    {
        var prompt = new ModelsPrompt();
        var list = OpenAiTools.CreateToolsJsonForResponsesApi(prompt, "codex-8k");
        Assert.Single(list);
        var tool = list[0];
        Assert.Equal("local_shell", tool.GetProperty("type").GetString());
    }

    [Fact]
    public void ChatCompletionsApi_TransformsFormat()
    {
        var prompt = new ModelsPrompt();
        var list = OpenAiTools.CreateToolsJsonForChatCompletionsApi(prompt, "gpt-4");
        Assert.Single(list);
        var tool = list[0];
        Assert.Equal("function", tool.GetProperty("type").GetString());
        Assert.Equal("shell", tool.GetProperty("function").GetProperty("name").GetString());
    }
}
