using CodexCli.Config;

namespace CodexCli.Util;

public static class EnvUtils
{
    public static string FindCodexHome()
    {
        var env = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (!string.IsNullOrEmpty(env))
        {
            if (!Directory.Exists(env))
                throw new DirectoryNotFoundException(env);
            return Path.GetFullPath(env);
        }
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".codex");
    }

    public static string GetLogDir(AppConfig cfg)
    {
        var env = Environment.GetEnvironmentVariable("CODEX_LOG_DIR");
        if (!string.IsNullOrEmpty(env))
            return env;
        return Path.Combine(cfg.CodexHome ?? FindCodexHome(), "log");
    }

    public static string GetHistoryDir(AppConfig? cfg = null)
    {
        var env = Environment.GetEnvironmentVariable("CODEX_HISTORY_DIR");
        if (!string.IsNullOrEmpty(env))
            return env;
        var home = cfg?.CodexHome ?? FindCodexHome();
        return Path.Combine(home, "history");
    }

    public static string GetLogLevel(string? cliLevel = null)
    {
        if (!string.IsNullOrEmpty(cliLevel))
            return cliLevel;
        var env = Environment.GetEnvironmentVariable("CODEX_LOG_LEVEL");
        if (!string.IsNullOrEmpty(env))
            return env;
        return "Information";
    }
}
