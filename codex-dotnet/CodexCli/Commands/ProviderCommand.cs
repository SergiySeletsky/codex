using System.CommandLine;
using CodexCli.Config;
using CodexCli.Util;
using System.Collections.Generic;
using System.IO;
using Tomlyn;

namespace CodexCli.Commands;

public static class ProviderCommand
{
    public static Command Create(Option<string?> configOption)
    {
        var list = new Command("list", "List available providers");
        var namesOnlyOpt = new Option<bool>("--names-only", () => false, "Print only provider ids");
        var verboseOpt = new Option<bool>("--verbose", () => false, "Include env key instructions");
        var jsonListOpt = new Option<bool>("--json", description: "Output JSON list");
        list.AddOption(namesOnlyOpt);
        list.AddOption(verboseOpt);
        list.AddOption(jsonListOpt);
        list.SetHandler((string? cfgPath, bool namesOnly, bool verbose, bool json) =>
        {
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);
            var providers = cfg?.ModelProviders ?? ModelProviderInfo.BuiltIns;
            if (json)
            {
                if (namesOnly)
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(providers.Keys));
                else
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                        providers.Select(p => new { id = p.Key, p.Value.Name, p.Value.BaseUrl, env_key = p.Value.EnvKey }),
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                return;
            }
            foreach (var (id, info) in providers)
            {
                if (namesOnly)
                    Console.WriteLine(id);
                else
                {
                    Console.WriteLine($"{id}\t{info.Name}\t{info.BaseUrl}");
                    if (verbose)
                    {
                        if (!string.IsNullOrEmpty(info.EnvKey))
                            Console.WriteLine($"  env: {info.EnvKey}");
                        if (!string.IsNullOrEmpty(info.EnvKeyInstructions))
                            Console.WriteLine($"  {info.EnvKeyInstructions}");
                    }
                }
            }
        }, configOption, namesOnlyOpt, verboseOpt, jsonListOpt);

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

        var addCmd = new Command("add", "Add a provider");
        var addId = new Argument<string>("id");
        var nameOpt = new Option<string>("--name") { IsRequired = true };
        var baseOpt = new Option<string>("--base-url") { IsRequired = true };
        var envOpt = new Option<string?>("--env-key");
        addCmd.AddArgument(addId);
        addCmd.AddOption(nameOpt);
        addCmd.AddOption(baseOpt);
        addCmd.AddOption(envOpt);
        addCmd.SetHandler((string id, string name, string baseUrl, string? envKey, string? cfgPath) =>
        {
            if (string.IsNullOrEmpty(cfgPath))
            {
                Console.Error.WriteLine("--config required");
                return;
            }
            var model = File.Exists(cfgPath) ? Toml.ToModel(File.ReadAllText(cfgPath)) as IDictionary<string, object?> : new Dictionary<string, object?>();
            if (!model.TryGetValue("model_providers", out var mpObj) || mpObj is not IDictionary<string, object?> mp)
            {
                mp = new Dictionary<string, object?>();
                model["model_providers"] = mp;
            }
            var prov = new Dictionary<string, object?>
            {
                ["name"] = name,
                ["base_url"] = baseUrl
            };
            if (envKey != null) prov["env_key"] = envKey;
            mp[id] = prov;
            File.WriteAllText(cfgPath, Toml.FromModel(model));
        }, addId, nameOpt, baseOpt, envOpt, configOption);

        var removeCmd = new Command("remove", "Remove a provider");
        var remId = new Argument<string>("id");
        removeCmd.AddArgument(remId);
        removeCmd.SetHandler((string id, string? cfgPath) =>
        {
            if (string.IsNullOrEmpty(cfgPath) || !File.Exists(cfgPath)) return;
            var model = Toml.ToModel(File.ReadAllText(cfgPath)) as IDictionary<string, object?> ?? new Dictionary<string, object?>();
            if (model.TryGetValue("model_providers", out var mpObj) && mpObj is IDictionary<string, object?> mp)
            {
                mp.Remove(id);
                File.WriteAllText(cfgPath, Toml.FromModel(model));
            }
        }, remId, configOption);

        var setCmd = new Command("set-default", "Set default provider");
        var setId = new Argument<string>("id");
        setCmd.AddArgument(setId);
        setCmd.SetHandler((string id, string? cfgPath) =>
        {
            if (string.IsNullOrEmpty(cfgPath))
            {
                Console.Error.WriteLine("--config required");
                return;
            }
            var model = File.Exists(cfgPath) ? Toml.ToModel(File.ReadAllText(cfgPath)) as IDictionary<string, object?> : new Dictionary<string, object?>();
            model["model_provider"] = id;
            File.WriteAllText(cfgPath, Toml.FromModel(model));
        }, setId, configOption);

        var loginCmd = new Command("login", "Store API key for a provider");
        var loginId = new Argument<string>("id");
        var keyOpt = new Option<string?>("--api-key", "API key to save");
        loginCmd.AddArgument(loginId);
        loginCmd.AddOption(keyOpt);
        loginCmd.SetHandler((string id, string? key) =>
        {
            var info = ModelProviderInfo.BuiltIns.TryGetValue(id, out var p) ? p : null;
            if (string.IsNullOrWhiteSpace(key))
            {
                if (info != null) ApiKeyManager.PrintEnvInstructions(info);
                else Console.WriteLine($"Provide API key via --api-key for {id}");
                return;
            }
            ApiKeyManager.SaveKey(id, key);
            Console.WriteLine($"Saved API key for {id}");
        }, loginId, keyOpt);

        var logoutCmd = new Command("logout", "Remove API key for a provider");
        var logoutId = new Argument<string>("id");
        logoutCmd.AddArgument(logoutId);
        logoutCmd.SetHandler((string id) =>
        {
            if (ApiKeyManager.DeleteKey(id))
                Console.WriteLine($"Removed API key for {id}");
            else
                Console.WriteLine($"No stored API key for {id}");
        }, logoutId);

        var keysCmd = new Command("keys", "List stored API keys");
        keysCmd.SetHandler(() =>
        {
            foreach (var k in ApiKeyManager.ListKeys())
                Console.WriteLine(k);
        });

        var updateCmd = new Command("update", "Update provider details");
        var updId = new Argument<string>("id");
        var updNameOpt = new Option<string?>("--name");
        var updBaseOpt = new Option<string?>("--base-url");
        var updEnvOpt = new Option<string?>("--env-key");
        updateCmd.AddArgument(updId);
        updateCmd.AddOption(updNameOpt);
        updateCmd.AddOption(updBaseOpt);
        updateCmd.AddOption(updEnvOpt);
        updateCmd.AddOption(configOption);
        updateCmd.SetHandler((string id, string? name, string? baseUrl, string? envKey, string? cfgPath) =>
        {
            if (string.IsNullOrEmpty(cfgPath) || !File.Exists(cfgPath)) return;
            var model = Toml.ToModel(File.ReadAllText(cfgPath)) as IDictionary<string, object?> ?? new Dictionary<string, object?>();
            if (!model.TryGetValue("model_providers", out var mpObj) || mpObj is not IDictionary<string, object?> mp)
                return;
            if (mp.TryGetValue(id, out var obj) && obj is IDictionary<string, object?> prov)
            {
                if (name != null) prov["name"] = name;
                if (baseUrl != null) prov["base_url"] = baseUrl;
                if (envKey != null) prov["env_key"] = envKey;
                File.WriteAllText(cfgPath, Toml.FromModel(model));
            }
        }, updId, updNameOpt, updBaseOpt, updEnvOpt, configOption);

        var cmd = new Command("provider", "Provider utilities");
        cmd.AddCommand(list);
        cmd.AddCommand(infoCmd);
        cmd.AddCommand(currentCmd);
        cmd.AddCommand(addCmd);
        cmd.AddCommand(removeCmd);
        cmd.AddCommand(setCmd);
        cmd.AddCommand(loginCmd);
        cmd.AddCommand(logoutCmd);
        cmd.AddCommand(keysCmd);
        cmd.AddCommand(updateCmd);
        return cmd;
    }
}
