using System.CommandLine;
using CodexCli.Config;
using CodexCli.Util;

namespace CodexCli.Commands;

public static class LoginCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var overridesOpt = new Option<string[]>("-c") { AllowMultipleArgumentsPerToken = true, Description = "Config overrides" };
        var cmd = new Command("login", "Login with ChatGPT");
        cmd.AddOption(overridesOpt);
        cmd.SetHandler(async (string? cfgPath, string? cd, string[] ov) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);
            Console.Write("Paste access token: ");
            var token = Console.ReadLine();
            token ??= Environment.GetEnvironmentVariable("CODEX_TOKEN");
            if (!string.IsNullOrWhiteSpace(token))
            {
                TokenManager.SaveToken(token);
                Console.WriteLine("Token saved.");
            }

            Console.Write("OpenAI API key (optional): ");
            var apiKey = Console.ReadLine();
            apiKey ??= OpenAiKeyManager.GetKey();
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                OpenAiKeyManager.SetKey(apiKey);
                Console.WriteLine("API key set.");
            }
            var overrides = ConfigOverrides.Parse(ov);
            if (overrides.Overrides.Count > 0)
                Console.WriteLine($"{overrides.Overrides.Count} override(s) parsed");
            await Task.CompletedTask;
        }, configOption, cdOption, overridesOpt);
        return cmd;
    }
}
