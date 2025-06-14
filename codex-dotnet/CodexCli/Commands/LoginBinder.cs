using System.CommandLine;
using System.CommandLine.Binding;
using CodexCli.Config;

namespace CodexCli.Commands;

public class LoginBinder : BinderBase<LoginOptions>
{
    private readonly Option<string[]> _overrides;
    private readonly Option<string?> _token;
    private readonly Option<string?> _apiKey;
    private readonly Option<string?> _provider;
    private readonly Option<bool> _chatgpt;
    private readonly Option<ShellEnvironmentPolicyInherit?> _envInherit;
    private readonly Option<bool?> _envIgnore;
    private readonly Option<string[]> _envExclude;
    private readonly Option<string[]> _envSet;
    private readonly Option<string[]> _envInclude;

    public LoginBinder(Option<string[]> overridesOpt, Option<string?> tokenOpt, Option<string?> apiOpt,
        Option<string?> providerOpt, Option<bool> chatgptOpt,
        Option<ShellEnvironmentPolicyInherit?> envInheritOpt, Option<bool?> envIgnoreOpt,
        Option<string[]> envExcludeOpt, Option<string[]> envSetOpt, Option<string[]> envIncludeOpt)
    {
        _overrides = overridesOpt;
        _token = tokenOpt;
        _apiKey = apiOpt;
        _provider = providerOpt;
        _chatgpt = chatgptOpt;
        _envInherit = envInheritOpt;
        _envIgnore = envIgnoreOpt;
        _envExclude = envExcludeOpt;
        _envSet = envSetOpt;
        _envInclude = envIncludeOpt;
    }

    protected override LoginOptions GetBoundValue(BindingContext bindingContext)
    {
        return new LoginOptions(
            bindingContext.ParseResult.GetValueForOption(_overrides) ?? Array.Empty<string>(),
            bindingContext.ParseResult.GetValueForOption(_token),
            bindingContext.ParseResult.GetValueForOption(_apiKey),
            bindingContext.ParseResult.GetValueForOption(_provider),
            bindingContext.ParseResult.GetValueForOption(_chatgpt),
            bindingContext.ParseResult.GetValueForOption(_envInherit),
            bindingContext.ParseResult.GetValueForOption(_envIgnore),
            bindingContext.ParseResult.GetValueForOption(_envExclude) ?? Array.Empty<string>(),
            bindingContext.ParseResult.GetValueForOption(_envSet) ?? Array.Empty<string>(),
            bindingContext.ParseResult.GetValueForOption(_envInclude) ?? Array.Empty<string>()
        );
    }
}
