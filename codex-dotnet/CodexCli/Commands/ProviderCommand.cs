using System.CommandLine;
using CodexCli.Config;
using CodexCli.Util;

namespace CodexCli.Commands;

public static class ProviderCommand
{
    public static Command Create(Option<string?> configOption)
    {
        var list = new Command("list", "List available providers");
        list.SetHandler((string? cfgPath) =>
        {
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);
            var providers = cfg?.ModelProviders ?? ModelProviderInfo.BuiltIns;
            foreach (var (id, info) in providers)
            {
                Console.WriteLine($"{id}\t{info.Name}\t{info.BaseUrl}");
            }
        }, configOption);

        var infoCmd = new Command("info", "Show provider details");
        var idArg = new Argument<string>("id");
        infoCmd.AddArgument(idArg);
        infoCmd.SetHandler((string id, string? cfgPath) =>
        {
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);
            var providers = cfg?.ModelProviders ?? ModelProviderInfo.BuiltIns;
            if (providers.TryGetValue(id, out var info))
            {
                Console.WriteLine($"name: {info.Name}\nbase_url: {info.BaseUrl}");
                if (info.EnvKey != null)
                    Console.WriteLine($"env_key: {info.EnvKey}");
                if (!string.IsNullOrEmpty(info.EnvKeyInstructions))
                    Console.WriteLine(info.EnvKeyInstructions);
            }
            else
            {
                Console.WriteLine($"Provider {id} not found");
            }
        }, idArg, configOption);

        var currentCmd = new Command("current", "Show current provider");
        currentCmd.SetHandler((string? cfgPath) =>
        {
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);
            var id = EnvUtils.GetModelProviderId(cfg?.ModelProvider);
            Console.WriteLine(id ?? "openai");
        }, configOption);

        var cmd = new Command("provider", "Provider utilities");
        cmd.AddCommand(list);
        cmd.AddCommand(infoCmd);
        cmd.AddCommand(currentCmd);
        return cmd;
    }
}
