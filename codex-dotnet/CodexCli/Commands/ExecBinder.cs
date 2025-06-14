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
    private readonly Option<bool> _fullAuto;
    private readonly Option<string> _color;
    private readonly Option<string?> _lastMessage;
    private readonly Option<bool> _skipGit;
    private readonly Option<string[]> _overrides;

    public ExecBinder(Argument<string?> prompt, Option<FileInfo[]> images, Option<string?> model,
        Option<string?> profile, Option<bool> fullAuto, Option<string> color,
        Option<string?> lastMessage, Option<bool> skipGit, Option<string[]> overrides)
    {
        _prompt = prompt;
        _images = images;
        _model = model;
        _profile = profile;
        _fullAuto = fullAuto;
        _color = color;
        _lastMessage = lastMessage;
        _skipGit = skipGit;
        _overrides = overrides;
    }

    protected override ExecOptions GetBoundValue(BindingContext bindingContext)
    {
        return new ExecOptions(
            bindingContext.ParseResult.GetValueForArgument(_prompt),
            bindingContext.ParseResult.GetValueForOption(_images) ?? Array.Empty<FileInfo>(),
            bindingContext.ParseResult.GetValueForOption(_model),
            bindingContext.ParseResult.GetValueForOption(_profile),
            bindingContext.ParseResult.GetValueForOption(_fullAuto),
            bindingContext.ParseResult.GetValueForOption(_color) ?? "auto",
            bindingContext.ParseResult.GetValueForOption(_lastMessage),
            bindingContext.ParseResult.GetValueForOption(_skipGit),
            bindingContext.ParseResult.GetValueForOption(_overrides) ?? Array.Empty<string>()
        );
    }
}
