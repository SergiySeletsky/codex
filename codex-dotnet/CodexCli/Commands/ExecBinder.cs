using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;
using CodexCli.Config;

namespace CodexCli.Commands;

// Parsed approval flag uses ApprovalModeCliArg for parity with Rust CLI (tested in ExecBinderTests)

public class ExecBinder : BinderBase<ExecOptions>
{
    private readonly Argument<string?> _prompt;
    private readonly Option<FileInfo[]> _images;
    private readonly Option<string?> _model;
    private readonly Option<string?> _profile;
    private readonly Option<string?> _provider;
    private readonly Option<bool> _fullAuto;
    private readonly Option<ApprovalModeCliArg?> _approval;
    private readonly Option<string[]> _sandbox;
    private readonly Option<ColorMode> _color;
    private readonly Option<string?> _cwd;
    private readonly Option<string?> _lastMessage;
    private readonly Option<string?> _sessionId;
    private readonly Option<bool> _skipGit;
    private readonly Option<string[]> _notify;
    private readonly Option<string[]> _overrides;
    private readonly Option<ReasoningEffort?> _effort;
    private readonly Option<ReasoningSummary?> _summary;
    private readonly Option<string?> _instructions;
    private readonly Option<bool?> _hideReasoning;
    private readonly Option<bool?> _disableStorage;
    private readonly Option<bool> _noProjectDoc;
    private readonly Option<bool> _json;
    private readonly Option<string?> _eventLog;
    private readonly Option<ShellEnvironmentPolicyInherit?> _envInherit;
    private readonly Option<bool?> _envIgnore;
    private readonly Option<string[]> _envExclude;
    private readonly Option<string[]> _envSet;
    private readonly Option<string[]> _envInclude;
    private readonly Option<int?> _docMaxBytes;
    private readonly Option<string?> _docPath;
    private readonly Option<string?> _mcpServer;
    private readonly Option<string?> _eventsUrl;
    private readonly Option<bool> _watchEvents;

    public ExecBinder(Argument<string?> prompt, Option<FileInfo[]> images, Option<string?> model,
        Option<string?> profile, Option<string?> provider, Option<bool> fullAuto,
        Option<ApprovalModeCliArg?> approval, Option<string[]> sandbox, Option<ColorMode> color,
        Option<string?> cwd, Option<string?> lastMessage, Option<string?> sessionId, Option<bool> skipGit,
        Option<string[]> notify, Option<string[]> overrides, Option<ReasoningEffort?> effort,
        Option<ReasoningSummary?> summary, Option<string?> instructions,
        Option<bool?> hideReasoning, Option<bool?> disableStorage,
        Option<bool> noProjectDoc, Option<bool> json, Option<string?> eventLog,
        Option<ShellEnvironmentPolicyInherit?> envInherit, Option<bool?> envIgnore,
        Option<string[]> envExclude, Option<string[]> envSet, Option<string[]> envInclude,
        Option<int?> docMaxBytes, Option<string?> docPath, Option<string?> mcpServer,
        Option<string?> eventsUrl, Option<bool> watchEvents)
    {
        _prompt = prompt;
        _images = images;
        _model = model;
        _profile = profile;
        _provider = provider;
        _fullAuto = fullAuto;
        _approval = approval;
        _sandbox = sandbox;
        _color = color;
        _cwd = cwd;
        _lastMessage = lastMessage;
        _sessionId = sessionId;
        _skipGit = skipGit;
        _notify = notify;
        _overrides = overrides;
        _effort = effort;
        _summary = summary;
        _instructions = instructions;
        _hideReasoning = hideReasoning;
        _disableStorage = disableStorage;
        _noProjectDoc = noProjectDoc;
        _json = json;
        _eventLog = eventLog;
        _envInherit = envInherit;
        _envIgnore = envIgnore;
        _envExclude = envExclude;
        _envSet = envSet;
        _envInclude = envInclude;
        _docMaxBytes = docMaxBytes;
        _docPath = docPath;
        _mcpServer = mcpServer;
        _eventsUrl = eventsUrl;
        _watchEvents = watchEvents;
    }

    protected override ExecOptions GetBoundValue(BindingContext bindingContext)
    {
        var sandboxRaw = bindingContext.ParseResult.GetValueForOption(_sandbox) ?? Array.Empty<string>();
        var basePath = Environment.CurrentDirectory;
        var sandbox = sandboxRaw.Select(s => SandboxPermissionParser.Parse(s, basePath)).ToArray();

        return new ExecOptions(
            bindingContext.ParseResult.GetValueForArgument(_prompt),
            bindingContext.ParseResult.GetValueForOption(_images) ?? Array.Empty<FileInfo>(),
            bindingContext.ParseResult.GetValueForOption(_model),
            bindingContext.ParseResult.GetValueForOption(_profile),
            bindingContext.ParseResult.GetValueForOption(_provider),
            bindingContext.ParseResult.GetValueForOption(_fullAuto),
            bindingContext.ParseResult.GetValueForOption(_approval),
            sandbox,
            bindingContext.ParseResult.GetValueForOption(_color),
            bindingContext.ParseResult.GetValueForOption(_cwd),
            bindingContext.ParseResult.GetValueForOption(_lastMessage),
            bindingContext.ParseResult.GetValueForOption(_sessionId),
            bindingContext.ParseResult.GetValueForOption(_skipGit),
            bindingContext.ParseResult.GetValueForOption(_notify) ?? Array.Empty<string>(),
            bindingContext.ParseResult.GetValueForOption(_overrides) ?? Array.Empty<string>(),
            bindingContext.ParseResult.GetValueForOption(_effort),
            bindingContext.ParseResult.GetValueForOption(_summary),
            bindingContext.ParseResult.GetValueForOption(_instructions),
            bindingContext.ParseResult.GetValueForOption(_hideReasoning),
            bindingContext.ParseResult.GetValueForOption(_disableStorage),
            bindingContext.ParseResult.GetValueForOption(_noProjectDoc),
            bindingContext.ParseResult.GetValueForOption(_json),
            bindingContext.ParseResult.GetValueForOption(_eventLog),
            bindingContext.ParseResult.GetValueForOption(_envInherit),
            bindingContext.ParseResult.GetValueForOption(_envIgnore),
            bindingContext.ParseResult.GetValueForOption(_envExclude) ?? Array.Empty<string>(),
            bindingContext.ParseResult.GetValueForOption(_envSet) ?? Array.Empty<string>(),
            bindingContext.ParseResult.GetValueForOption(_envInclude) ?? Array.Empty<string>(),
            bindingContext.ParseResult.GetValueForOption(_docMaxBytes),
            bindingContext.ParseResult.GetValueForOption(_docPath),
            bindingContext.ParseResult.GetValueForOption(_mcpServer),
            bindingContext.ParseResult.GetValueForOption(_eventsUrl),
            bindingContext.ParseResult.GetValueForOption(_watchEvents)
        );
    }
}
