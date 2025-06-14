using CodexCli.Commands;
using Tomlyn;
using System.Linq;

namespace CodexCli.Config;

public class AppConfig
{
    public string[]? NotifyCommand { get; set; }
    public string? Model { get; set; }
    public string? ModelProvider { get; set; }
    public string? CodexHome { get; set; }
    public string? Instructions { get; set; }
    public Dictionary<string, ConfigProfile> Profiles { get; set; } = new();

    public static AppConfig Load(string path, string? profile = null)
    {
        var text = File.ReadAllText(path);
        var model = Toml.ToModel(text) as IDictionary<string, object?> ?? new Dictionary<string, object?>();
        var cfg = new AppConfig();
        if (model.TryGetValue("notify", out var notify) && notify is object[] arr)
            cfg.NotifyCommand = arr.Select(o => o?.ToString() ?? string.Empty).ToArray();
        if (model.TryGetValue("model", out var m)) cfg.Model = m?.ToString();
        if (model.TryGetValue("model_provider", out var mp)) cfg.ModelProvider = mp?.ToString();
        if (model.TryGetValue("codex_home", out var h)) cfg.CodexHome = h?.ToString();
        if (model.TryGetValue("instructions", out var inst)) cfg.Instructions = inst?.ToString();
        if (model.TryGetValue("profiles", out var profs) && profs is IDictionary<string, object?> pmap)
        {
            foreach (var (k, v) in pmap)
            {
                if (v is IDictionary<string, object?> pm)
                {
                    var cp = new ConfigProfile();
                    if (pm.TryGetValue("model", out var m2)) cp.Model = m2?.ToString();
                    if (pm.TryGetValue("model_provider", out var mp2)) cp.ModelProvider = mp2?.ToString();
                    if (pm.TryGetValue("approval_policy", out var ap) && Enum.TryParse<ApprovalMode>(ap?.ToString(), true, out var apv)) cp.ApprovalPolicy = apv;
                    if (pm.TryGetValue("disable_response_storage", out var drs))
                        cp.DisableResponseStorage = Toml.ToModel($"_x_ = {drs}").TryGetValue("_x_", out var val) && bool.TryParse(val?.ToString(), out var b) ? b : (bool?)null;
                    cfg.Profiles[k] = cp;
                }
            }
        }
        cfg.CodexHome ??= Environment.GetEnvironmentVariable("CODEX_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
        if (cfg.Instructions == null)
        {
            var instPath = Path.Combine(cfg.CodexHome, "instructions.md");
            if (File.Exists(instPath))
                cfg.Instructions = File.ReadAllText(instPath);
        }
        if (profile != null && cfg.Profiles.TryGetValue(profile, out var p))
        {
            if (p.Model != null) cfg.Model = p.Model;
            if (p.ModelProvider != null) cfg.ModelProvider = p.ModelProvider;
        }
        return cfg;
    }
}
