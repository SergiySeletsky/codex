// Ported from codex-rs/cli/src/login.rs (done)
using System.CommandLine;
using CodexCli.Config;
using CodexCli.Util;

namespace CodexCli.Commands;

public static class LoginCommand
{
    public static Func<string, bool, IDictionary<string, string>?, Task<string>> LoginWithChatGptAsync { get; set; } = ChatGptLogin.LoginAsync;

    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var overridesOpt = new Option<string[]>("-c") { AllowMultipleArgumentsPerToken = true, Description = "Config overrides" };
        var tokenOpt = new Option<string?>("--token", "Token to save");
        var apiOpt = new Option<string?>("--api-key", "API key to save");
        var providerOpt = new Option<string?>("--provider", "Provider id for API key");
        var chatgptOpt = new Option<bool>("--chatgpt", () => false, "Use ChatGPT browser login");
        var envInheritOpt = new Option<ShellEnvironmentPolicyInherit?>("--env-inherit");
        var envIgnoreOpt = new Option<bool?>("--env-ignore-default-excludes");
        var envExcludeOpt = new Option<string[]>("--env-exclude") { AllowMultipleArgumentsPerToken = true };
        var envSetOpt = new Option<string[]>("--env-set") { AllowMultipleArgumentsPerToken = true };
        var envIncludeOpt = new Option<string[]>("--env-include-only") { AllowMultipleArgumentsPerToken = true };
        var cmd = new Command("login", "Login with ChatGPT");
        cmd.AddOption(overridesOpt);
        cmd.AddOption(tokenOpt);
        cmd.AddOption(apiOpt);
        cmd.AddOption(providerOpt);
        cmd.AddOption(chatgptOpt);
        cmd.AddOption(envInheritOpt);
        cmd.AddOption(envIgnoreOpt);
        cmd.AddOption(envExcludeOpt);
        cmd.AddOption(envSetOpt);
        cmd.AddOption(envIncludeOpt);
        var binder = new LoginBinder(overridesOpt, tokenOpt, apiOpt, providerOpt, chatgptOpt,
            envInheritOpt, envIgnoreOpt, envExcludeOpt, envSetOpt, envIncludeOpt);

        cmd.SetHandler(async (LoginOptions opts, string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);
            var token = opts.Token;
            if (token == null)
            {
                Console.Write("Paste access token: ");
                token = Console.ReadLine();
            }
            token ??= Environment.GetEnvironmentVariable("CODEX_TOKEN");
            if (!string.IsNullOrWhiteSpace(token))
            {
                TokenManager.SaveToken(token);
                Console.WriteLine("Token saved.");
            }

            var provider = EnvUtils.GetModelProviderId(opts.Provider) ?? "openai";
            var apiKey = opts.ApiKey;
            if (apiKey == null)
            {
                Console.Write($"{provider} API key (optional): ");
                apiKey = Console.ReadLine();
            }
            var provInfo = cfg?.GetProvider(provider) ?? ModelProviderInfo.BuiltIns[provider];
            var policy = cfg?.ShellEnvironmentPolicy ?? new ShellEnvironmentPolicy();
            if (opts.EnvInherit != null) policy.Inherit = opts.EnvInherit.Value;
            if (opts.EnvIgnoreDefaultExcludes != null) policy.IgnoreDefaultExcludes = opts.EnvIgnoreDefaultExcludes.Value;
            if (opts.EnvExclude.Length > 0) policy.Exclude = opts.EnvExclude.Select(EnvironmentVariablePattern.CaseInsensitive).ToList();
            if (opts.EnvSet.Length > 0)
                policy.Set = opts.EnvSet.Select(s => s.Split('=',2)).ToDictionary(p=>p[0], p=>p.Length>1?p[1]:string.Empty);
            if (opts.EnvIncludeOnly.Length > 0) policy.IncludeOnly = opts.EnvIncludeOnly.Select(EnvironmentVariablePattern.CaseInsensitive).ToList();
            var envMap = ExecEnv.Create(policy);
            apiKey ??= ApiKeyManager.GetKey(provInfo);
            if (string.IsNullOrWhiteSpace(apiKey) && opts.ChatGpt)
            {
                Console.WriteLine("Launching browser login...");
                try
                {
                    apiKey = await LoginWithChatGptAsync(EnvUtils.FindCodexHome(), false, envMap);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                ApiKeyManager.SaveKey(provider, apiKey);
                Console.WriteLine("API key saved.");
            }
            else if (!string.IsNullOrEmpty(provInfo.EnvKeyInstructions))
            {
                Console.WriteLine(provInfo.EnvKeyInstructions);
            }
            var overrides = ConfigOverrides.Parse(opts.Overrides);
            if (overrides.Overrides.Count > 0)
                Console.WriteLine($"{overrides.Overrides.Count} override(s) parsed");
            await Task.CompletedTask;
        }, binder, configOption, cdOption);
        return cmd;
    }
}
