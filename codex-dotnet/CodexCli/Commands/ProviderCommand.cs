using System.CommandLine;
using CodexCli.Config;

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

        var cmd = new Command("provider", "Provider utilities");
        cmd.AddCommand(list);
        return cmd;
    }
}
