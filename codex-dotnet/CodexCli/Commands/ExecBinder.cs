using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;

namespace CodexCli.Commands;

public class ExecBinder : BinderBase<ExecOptions>
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
    private readonly Option<string?> _cwd;
    private readonly Option<string?> _lastMessage;
    private readonly Option<bool> _skipGit;
    private readonly Option<string[]> _notify;
    private readonly Option<string[]> _overrides;

    public ExecBinder(Argument<string?> prompt, Option<FileInfo[]> images, Option<string?> model,
        Option<string?> profile, Option<string?> provider, Option<bool> fullAuto,
        Option<ApprovalMode?> approval, Option<string[]> sandbox, Option<ColorMode> color,
        Option<string?> cwd, Option<string?> lastMessage, Option<bool> skipGit,
        Option<string[]> notify, Option<string[]> overrides)
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
        _skipGit = skipGit;
        _notify = notify;
        _overrides = overrides;
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
            bindingContext.ParseResult.GetValueForOption(_skipGit),
            bindingContext.ParseResult.GetValueForOption(_notify) ?? Array.Empty<string>(),
            bindingContext.ParseResult.GetValueForOption(_overrides) ?? Array.Empty<string>()
        );
    }
}
