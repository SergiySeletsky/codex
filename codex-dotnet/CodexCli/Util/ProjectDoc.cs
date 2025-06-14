using CodexCli.Config;

namespace CodexCli.Util;

public static class ProjectDoc
{
    private const int MaxBytes = 32 * 1024;
    private const string Separator = "\n\n--- project-doc ---\n\n";

    private static string? LoadFirstCandidate(string dir)
    {
        var candidate = Path.Combine(dir, "AGENTS.md");
        if (!File.Exists(candidate)) return null;
        var bytes = File.ReadAllBytes(candidate);
        if (bytes.Length > MaxBytes)
            bytes = bytes.Take(MaxBytes).ToArray();
        var text = System.Text.Encoding.UTF8.GetString(bytes).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string? FindProjectDoc(string cwd)
    {
        var dir = new DirectoryInfo(cwd);
        var repoRoot = GitUtils.GetRepoRoot(cwd);
        while (dir != null)
        {
            var doc = LoadFirstCandidate(dir.FullName);
            if (doc != null) return doc;
            if (repoRoot != null && dir.FullName == repoRoot) break;
            dir = dir.Parent;
        }
        return null;
    }

    public static string? GetUserInstructions(AppConfig cfg, string cwd)
    {
        var inst = cfg.Instructions;
        var doc = FindProjectDoc(cwd);
        if (inst != null && doc != null)
            return inst.Trim() + Separator + doc.Trim();
        return inst ?? doc;
    }
}
