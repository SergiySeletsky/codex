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
        return Path.Combine(cfg.CodexHome ?? FindCodexHome(), "log");
    }
}
