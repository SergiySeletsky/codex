// Ported from codex-rs/core/src/util.rs (done)
namespace CodexCli.Util;

public static class GitUtils
{
    public static bool IsInsideGitRepo(string directory)
    {
        return GetRepoRoot(directory) != null;
    }

    public static string? GetRepoRoot(string directory)
    {
        var dir = new DirectoryInfo(directory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}
