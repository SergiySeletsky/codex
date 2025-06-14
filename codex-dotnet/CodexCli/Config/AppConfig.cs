using Tomlyn;

namespace CodexCli.Config;

public class AppConfig
{
    public string? NotifyCommand { get; set; }
    public string? Model { get; set; }

    public static AppConfig Load(string path)
    {
        var text = File.ReadAllText(path);
        var model = Toml.ToModel(text);
        var cfg = new AppConfig();
        if (model.TryGetValue("notify", out var notify)) cfg.NotifyCommand = notify?.ToString();
        if (model.TryGetValue("model", out var m)) cfg.Model = m?.ToString();
        return cfg;
    }
}
