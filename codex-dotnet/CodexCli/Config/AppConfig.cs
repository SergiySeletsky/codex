using Tomlyn;
using System.Linq;

namespace CodexCli.Config;

public class AppConfig
{
    public string[]? NotifyCommand { get; set; }
    public string? Model { get; set; }
    public string? CodexHome { get; set; }

    public static AppConfig Load(string path)
    {
        var text = File.ReadAllText(path);
        var model = Toml.ToModel(text);
        var cfg = new AppConfig();
        if (model.TryGetValue("notify", out var notify) && notify is object[] arr)
            cfg.NotifyCommand = arr.Select(o => o?.ToString() ?? string.Empty).ToArray();
        if (model.TryGetValue("model", out var m)) cfg.Model = m?.ToString();
        if (model.TryGetValue("codex_home", out var h)) cfg.CodexHome = h?.ToString();
        cfg.CodexHome ??= Environment.GetEnvironmentVariable("CODEX_HOME") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex");
        return cfg;
    }
}
