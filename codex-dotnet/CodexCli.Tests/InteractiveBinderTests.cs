using System.CommandLine;
using CodexCli.Commands;
using System.IO;

namespace CodexCli.Tests;

public class InteractiveBinderTests
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
        var skipGitOpt = new Option<bool>("--skip-git-repo-check");
        var cwdOpt = new Option<string?>("--cwd");
        var notifyOpt = new Option<string[]>("--notify") { AllowMultipleArgumentsPerToken = true };
        var overridesOpt = new Option<string[]>("-c") { AllowMultipleArgumentsPerToken = true };

        var binder = new InteractiveBinder(promptArg, imagesOpt, modelOpt, profileOpt, providerOpt,
            fullAutoOpt, approvalOpt, sandboxOpt, colorOpt, skipGitOpt, cwdOpt, notifyOpt, overridesOpt);

        var cmd = new Command("interactive");
        cmd.AddArgument(promptArg);
        cmd.AddOption(fullAutoOpt);
        cmd.AddOption(skipGitOpt);
        InteractiveOptions? captured = null;
        cmd.SetHandler((InteractiveOptions o) => captured = o, binder);
        var root = new RootCommand();
        root.AddCommand(cmd);

        await root.InvokeAsync("interactive hello --full-auto --skip-git-repo-check");

        Assert.NotNull(captured);
        Assert.True(captured!.FullAuto);
        Assert.True(captured.SkipGitRepoCheck);
        Assert.Equal("hello", captured.Prompt);
    }
}
