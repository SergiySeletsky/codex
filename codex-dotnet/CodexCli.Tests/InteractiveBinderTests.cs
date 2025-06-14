using System.CommandLine;
using CodexCli.Commands;
using System.IO;
using CodexCli.Config;

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
        var effortOpt = new Option<ReasoningEffort?>("--reasoning-effort");
        var summaryOpt = new Option<ReasoningSummary?>("--reasoning-summary");
        var instrOpt = new Option<string?>("--instructions");
        var hideReasonOpt = new Option<bool?>("--hide-agent-reasoning");
        var disableStorageOpt = new Option<bool?>("--disable-response-storage");
        var noProjDocOpt = new Option<bool>("--no-project-doc");
        var lastMsgOpt = new Option<string?>("--output-last-message");
        var logOpt = new Option<string?>("--event-log");
        var envInheritOpt = new Option<ShellEnvironmentPolicyInherit?>("--env-inherit");
        var envIgnoreOpt = new Option<bool?>("--env-ignore-default-excludes");
        var envExcludeOpt = new Option<string[]>("--env-exclude") { AllowMultipleArgumentsPerToken = true };
        var envSetOpt = new Option<string[]>("--env-set") { AllowMultipleArgumentsPerToken = true };
        var envIncludeOpt = new Option<string[]>("--env-include-only") { AllowMultipleArgumentsPerToken = true };

        var binder = new InteractiveBinder(promptArg, imagesOpt, modelOpt, profileOpt, providerOpt,
            fullAutoOpt, approvalOpt, sandboxOpt, colorOpt, skipGitOpt, cwdOpt, notifyOpt, overridesOpt,
            effortOpt, summaryOpt, instrOpt, hideReasonOpt, disableStorageOpt, lastMsgOpt, noProjDocOpt, logOpt,
            envInheritOpt, envIgnoreOpt, envExcludeOpt, envSetOpt, envIncludeOpt);

        var cmd = new Command("interactive");
        cmd.AddArgument(promptArg);
        cmd.AddOption(fullAutoOpt);
        cmd.AddOption(skipGitOpt);
        cmd.AddOption(hideReasonOpt);
        cmd.AddOption(disableStorageOpt);
        cmd.AddOption(noProjDocOpt);
        cmd.AddOption(lastMsgOpt);
        cmd.AddOption(logOpt);
        cmd.AddOption(envInheritOpt);
        cmd.AddOption(envIgnoreOpt);
        cmd.AddOption(envExcludeOpt);
        cmd.AddOption(envSetOpt);
        cmd.AddOption(envIncludeOpt);
        InteractiveOptions? captured = null;
        cmd.SetHandler((InteractiveOptions o) => captured = o, binder);
        var root = new RootCommand();
        root.AddCommand(cmd);

        await root.InvokeAsync("interactive hello --full-auto --skip-git-repo-check --hide-agent-reasoning --disable-response-storage --no-project-doc --event-log t.log --env-inherit none --env-exclude SECRET --env-set F=2 --env-include-only PATH");

        Assert.NotNull(captured);
        Assert.True(captured!.FullAuto);
        Assert.True(captured.SkipGitRepoCheck);
        Assert.Equal("hello", captured.Prompt);
        Assert.True(captured.HideAgentReasoning);
        Assert.True(captured.DisableResponseStorage);
        Assert.True(captured.NoProjectDoc);
        Assert.Equal("t.log", captured.EventLogFile);
        Assert.Equal(ShellEnvironmentPolicyInherit.None, captured.EnvInherit);
        Assert.Contains("SECRET", captured.EnvExclude);
        Assert.Contains("F=2", captured.EnvSet);
        Assert.Contains("PATH", captured.EnvIncludeOnly);
    }
}
