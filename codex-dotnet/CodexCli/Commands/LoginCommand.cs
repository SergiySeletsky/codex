using System.CommandLine;
using CodexCli.Config;
using CodexCli.Util;

namespace CodexCli.Commands;

public static class LoginCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var cmd = new Command("login", "Login with ChatGPT");
        cmd.SetHandler(async (string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);
            Console.Write("Paste access token: ");
            var token = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(token))
            {
                TokenManager.SaveToken(token);
                Console.WriteLine("Token saved.");
            }
            else
            {
                Console.WriteLine("No token provided.");
            }
            await Task.CompletedTask;
        }, configOption, cdOption);
        return cmd;
    }
}
