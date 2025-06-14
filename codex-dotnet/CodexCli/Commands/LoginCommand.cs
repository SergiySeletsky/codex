using System.CommandLine;
using CodexCli.Config;
using CodexCli.Util;

namespace CodexCli.Commands;

public static class LoginCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var overridesOpt = new Option<string[]>("-c") { AllowMultipleArgumentsPerToken = true, Description = "Config overrides" };
        var tokenOpt = new Option<string?>("--token", "Token to save");
        var apiOpt = new Option<string?>("--api-key", "API key to save");
        var providerOpt = new Option<string?>("--provider", () => "openai", "Provider id for API key");
        var cmd = new Command("login", "Login with ChatGPT");
        cmd.AddOption(overridesOpt);
        cmd.AddOption(tokenOpt);
        cmd.AddOption(apiOpt);
        cmd.AddOption(providerOpt);
        cmd.SetHandler(async (string? cfgPath, string? cd, string[] ov, string? tokenArg, string? apiArg, string provider) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);
            var token = tokenArg;
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

            var apiKey = apiArg;
            if (apiKey == null)
            {
                Console.Write($"{provider} API key (optional): ");
                apiKey = Console.ReadLine();
            }
            var provInfo = cfg?.GetProvider(provider) ?? ModelProviderInfo.BuiltIns[provider];
            apiKey ??= ApiKeyManager.GetKey(provInfo);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                ApiKeyManager.SaveKey(provider, apiKey);
                Console.WriteLine("API key saved.");
            }
            var overrides = ConfigOverrides.Parse(ov);
            if (overrides.Overrides.Count > 0)
                Console.WriteLine($"{overrides.Overrides.Count} override(s) parsed");
            await Task.CompletedTask;
        }, configOption, cdOption, overridesOpt, tokenOpt, apiOpt, providerOpt);
        return cmd;
    }
}
