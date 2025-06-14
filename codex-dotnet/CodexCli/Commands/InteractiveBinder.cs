using System.CommandLine;
using System.CommandLine.Binding;

namespace CodexCli.Commands;

public class InteractiveBinder : BinderBase<InteractiveOptions>
{
    private readonly Argument<string?> _prompt;
    private readonly Option<FileInfo[]> _images;
    private readonly Option<string?> _model;
    private readonly Option<string?> _profile;
    private readonly Option<bool> _fullAuto;
    private readonly Option<ApprovalMode?> _approval;
    private readonly Option<SandboxPermission[]> _sandbox;
    private readonly Option<bool> _skipGit;
    private readonly Option<string?> _cwd;

    public InteractiveBinder(Argument<string?> prompt, Option<FileInfo[]> images, Option<string?> model,
        Option<string?> profile, Option<bool> fullAuto, Option<ApprovalMode?> approval,
        Option<SandboxPermission[]> sandbox, Option<bool> skipGit, Option<string?> cwd)
    {
        _prompt = prompt;
        _images = images;
        _model = model;
        _profile = profile;
        _fullAuto = fullAuto;
        _approval = approval;
        _sandbox = sandbox;
        _skipGit = skipGit;
        _cwd = cwd;
    }

    protected override InteractiveOptions GetBoundValue(BindingContext bindingContext)
    {
        return new InteractiveOptions(
            bindingContext.ParseResult.GetValueForArgument(_prompt),
            bindingContext.ParseResult.GetValueForOption(_images) ?? Array.Empty<FileInfo>(),
            bindingContext.ParseResult.GetValueForOption(_model),
            bindingContext.ParseResult.GetValueForOption(_profile),
            bindingContext.ParseResult.GetValueForOption(_fullAuto),
            bindingContext.ParseResult.GetValueForOption(_approval),
            bindingContext.ParseResult.GetValueForOption(_sandbox) ?? Array.Empty<SandboxPermission>(),
            bindingContext.ParseResult.GetValueForOption(_skipGit),
            bindingContext.ParseResult.GetValueForOption(_cwd)
        );
    }
}

