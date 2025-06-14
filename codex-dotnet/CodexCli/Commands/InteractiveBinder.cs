using System.CommandLine;
using System.CommandLine.Binding;

namespace CodexCli.Commands;

public class InteractiveBinder : BinderBase<InteractiveOptions>
{
    private readonly Argument<string?> _prompt;
    private readonly Option<FileInfo[]> _images;
    private readonly Option<string?> _model;
    private readonly Option<string?> _profile;
    private readonly Option<string?> _provider;
    private readonly Option<bool> _fullAuto;
    private readonly Option<ApprovalMode?> _approval;
    private readonly Option<string[]> _sandbox;
    private readonly Option<ColorMode> _color;
    private readonly Option<bool> _skipGit;
    private readonly Option<string?> _cwd;
    private readonly Option<string[]> _notify;
    private readonly Option<string[]> _overrides;
    private readonly Option<ReasoningEffort?> _effort;
    private readonly Option<ReasoningSummary?> _summary;
    private readonly Option<string?> _instructions;
    private readonly Option<bool?> _hideReasoning;
    private readonly Option<bool?> _disableStorage;

    public InteractiveBinder(Argument<string?> prompt, Option<FileInfo[]> images, Option<string?> model,
        Option<string?> profile, Option<string?> provider, Option<bool> fullAuto, Option<ApprovalMode?> approval,
        Option<string[]> sandbox, Option<ColorMode> color, Option<bool> skipGit, Option<string?> cwd,
        Option<string[]> notify, Option<string[]> overrides, Option<ReasoningEffort?> effort,
        Option<ReasoningSummary?> summary, Option<string?> instructions,
        Option<bool?> hideReasoning, Option<bool?> disableStorage)
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
        _skipGit = skipGit;
        _cwd = cwd;
        _notify = notify;
        _overrides = overrides;
        _effort = effort;
        _summary = summary;
        _instructions = instructions;
        _hideReasoning = hideReasoning;
        _disableStorage = disableStorage;
    }

    protected override InteractiveOptions GetBoundValue(BindingContext bindingContext)
    {
        return new InteractiveOptions(
            bindingContext.ParseResult.GetValueForArgument(_prompt),
            bindingContext.ParseResult.GetValueForOption(_images) ?? Array.Empty<FileInfo>(),
            bindingContext.ParseResult.GetValueForOption(_model),
            bindingContext.ParseResult.GetValueForOption(_profile),
            bindingContext.ParseResult.GetValueForOption(_provider),
            bindingContext.ParseResult.GetValueForOption(_fullAuto),
            bindingContext.ParseResult.GetValueForOption(_approval),
            (bindingContext.ParseResult.GetValueForOption(_sandbox) ?? Array.Empty<string>())
                .Select(s => SandboxPermissionParser.Parse(s, Environment.CurrentDirectory)).ToArray(),
            bindingContext.ParseResult.GetValueForOption(_color),
            bindingContext.ParseResult.GetValueForOption(_skipGit),
            bindingContext.ParseResult.GetValueForOption(_cwd),
            bindingContext.ParseResult.GetValueForOption(_notify) ?? Array.Empty<string>(),
            bindingContext.ParseResult.GetValueForOption(_overrides) ?? Array.Empty<string>(),
            bindingContext.ParseResult.GetValueForOption(_effort),
            bindingContext.ParseResult.GetValueForOption(_summary),
            bindingContext.ParseResult.GetValueForOption(_instructions),
            bindingContext.ParseResult.GetValueForOption(_hideReasoning),
            bindingContext.ParseResult.GetValueForOption(_disableStorage)
        );
    }
}
