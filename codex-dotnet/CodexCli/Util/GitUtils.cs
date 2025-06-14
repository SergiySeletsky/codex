namespace CodexCli.Util;

public static class GitUtils
{
    public static bool IsInsideGitRepo(string directory)
    {
        var dir = new DirectoryInfo(directory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return true;
            dir = dir.Parent;
        }
        return false;
    }
}
