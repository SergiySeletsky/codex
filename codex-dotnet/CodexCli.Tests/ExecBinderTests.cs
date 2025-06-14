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
        var effortOpt = new Option<ReasoningEffort?>("--reasoning-effort");
        var summaryOpt = new Option<ReasoningSummary?>("--reasoning-summary");
        var instrOpt = new Option<string?>("--instructions");
        var hideReasonOpt = new Option<bool?>("--hide-agent-reasoning");
        var disableStorageOpt = new Option<bool?>("--disable-response-storage");
        var noProjDocOpt = new Option<bool>("--no-project-doc");
        var jsonOpt = new Option<bool>("--json");
        var logOpt = new Option<string?>("--event-log");

        var binder = new ExecBinder(promptArg, imagesOpt, modelOpt, profileOpt, providerOpt,
            fullAutoOpt, approvalOpt, sandboxOpt, colorOpt, cwdOpt, lastOpt, skipGitOpt,
            notifyOpt, overridesOpt, effortOpt, summaryOpt, instrOpt, hideReasonOpt, disableStorageOpt,
            noProjDocOpt, jsonOpt, logOpt);

        var cmd = new Command("exec");
        cmd.AddArgument(promptArg);
        cmd.AddOption(modelOpt);
        cmd.AddOption(fullAutoOpt);
        cmd.AddOption(hideReasonOpt);
        cmd.AddOption(disableStorageOpt);
        cmd.AddOption(noProjDocOpt);
        cmd.AddOption(jsonOpt);
        cmd.AddOption(logOpt);
        ExecOptions? captured = null;
        cmd.SetHandler((ExecOptions o) => captured = o, binder);
        var root = new RootCommand();
        root.AddCommand(cmd);

        await root.InvokeAsync("exec hello --model gpt-4 --full-auto --hide-agent-reasoning --disable-response-storage --no-project-doc --json --event-log log.txt");

        Assert.NotNull(captured);
        Assert.Equal("hello", captured!.Prompt);
        Assert.Equal("gpt-4", captured.Model);
        Assert.True(captured.FullAuto);
        Assert.True(captured.HideAgentReasoning);
        Assert.True(captured.DisableResponseStorage);
        Assert.True(captured.NoProjectDoc);
        Assert.True(captured.Json);
        Assert.Equal("log.txt", captured.EventLogFile);
    }
}
