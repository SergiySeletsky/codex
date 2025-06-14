using System.CommandLine;
using CodexCli.Commands;
using System.IO;

namespace CodexCli.Tests;

public class ExecBinderTests
{
    [Fact]
    public async Task BindsValues()
    {
        var promptArg = new Argument<string?>("prompt");
        var imagesOpt = new Option<FileInfo[]>("--image") { AllowMultipleArgumentsPerToken = true };
        var modelOpt = new Option<string?>("--model");
        var profileOpt = new Option<string?>("--profile");
        var providerOpt = new Option<string?>("--model-provider");
        var fullAutoOpt = new Option<bool>("--full-auto");
        var approvalOpt = new Option<ApprovalMode?>("--ask-for-approval");
        var sandboxOpt = new Option<string[]>("-s") { AllowMultipleArgumentsPerToken = true };
        var colorOpt = new Option<ColorMode>("--color", () => ColorMode.Auto);
        var cwdOpt = new Option<string?>("--cwd");
        var lastOpt = new Option<string?>("--output-last-message");
        var skipGitOpt = new Option<bool>("--skip-git-repo-check");
        var notifyOpt = new Option<string[]>("--notify") { AllowMultipleArgumentsPerToken = true };
        var overridesOpt = new Option<string[]>("-c") { AllowMultipleArgumentsPerToken = true };

        var binder = new ExecBinder(promptArg, imagesOpt, modelOpt, profileOpt, providerOpt,
            fullAutoOpt, approvalOpt, sandboxOpt, colorOpt, cwdOpt, lastOpt, skipGitOpt, notifyOpt, overridesOpt);

        var cmd = new Command("exec");
        cmd.AddArgument(promptArg);
        cmd.AddOption(modelOpt);
        cmd.AddOption(fullAutoOpt);
        ExecOptions? captured = null;
        cmd.SetHandler((ExecOptions o) => captured = o, binder);
        var root = new RootCommand();
        root.AddCommand(cmd);

        await root.InvokeAsync("exec hello --model gpt-4 --full-auto");

        Assert.NotNull(captured);
        Assert.Equal("hello", captured!.Prompt);
        Assert.Equal("gpt-4", captured.Model);
        Assert.True(captured.FullAuto);
    }
}
