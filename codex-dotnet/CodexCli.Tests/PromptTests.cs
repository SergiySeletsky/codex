using CodexCli.Models;
using Xunit;

public class PromptTests
{
    [Fact]
    public void FullInstructions_IncludesUserAndPatch()
    {
        var prompt = new Prompt { UserInstructions = "Be helpful" };
        var res = prompt.GetFullInstructions("gpt-4.1");
        Assert.Contains("Be helpful", res);
        Assert.Contains("apply_patch", res);
    }

    [Fact]
    public void FullInstructions_NoUser_NoPatch()
    {
        var prompt = new Prompt();
        var res = prompt.GetFullInstructions("codex");
        Assert.StartsWith("Please resolve", res);
        Assert.Contains("apply_patch", res);
        Assert.Contains("deployed coding agent", res);
    }
}
