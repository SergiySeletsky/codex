// Ported from codex-rs/core/src/project_doc.rs (done)
using CodexCli.Config;

namespace CodexCli.Util;

public static class ProjectDoc
{
    private const int DefaultMaxBytes = 32 * 1024;
    private const string Separator = "\n\n--- project-doc ---\n\n";

    private static string? LoadFile(string path, int maxBytes)
    {
        if (!File.Exists(path)) return null;
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length > maxBytes)
            bytes = bytes.Take(maxBytes).ToArray();
        var text = System.Text.Encoding.UTF8.GetString(bytes).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string? LoadFirstCandidate(string dir, int maxBytes)
    {
        var candidate = Path.Combine(dir, "AGENTS.md");
        return LoadFile(candidate, maxBytes);
    }

    private static string? FindProjectDoc(string cwd, int maxBytes)
    {
        var global = Path.Combine(EnvUtils.FindCodexHome(), "AGENTS.md");
        if (File.Exists(global))
        {
            var txt = LoadFile(global, maxBytes);
            if (!string.IsNullOrEmpty(txt)) return txt;
        }
        var dir = new DirectoryInfo(cwd);
        var repoRoot = GitUtils.GetRepoRoot(cwd);
        while (dir != null)
        {
            var doc = LoadFirstCandidate(dir.FullName, maxBytes);
            if (doc != null) return doc;
            if (repoRoot != null && dir.FullName == repoRoot) break;
            dir = dir.Parent;
        }
        return null;
    }

    public static string? GetUserInstructions(AppConfig cfg, string cwd, bool skipProjectDoc = false, int? maxBytesOverride = null, string? docPath = null)
    {
        var disable = skipProjectDoc || Environment.GetEnvironmentVariable("CODEX_DISABLE_PROJECT_DOC") == "1";
        int maxBytes = maxBytesOverride ?? cfg.ProjectDocMaxBytes;
        var inst = cfg.Instructions;
        string? doc;
        if (disable)
        {
            doc = null;
        }
        else if (docPath != null)
        {
            doc = LoadFile(docPath, maxBytes);
        }
        else
        {
            doc = FindProjectDoc(cwd, maxBytes);
        }
        if (inst != null && doc != null)
            return inst.Trim() + Separator + doc.Trim();
        return inst ?? doc;
    }
}
