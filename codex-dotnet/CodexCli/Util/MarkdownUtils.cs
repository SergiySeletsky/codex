using CodexCli.Config;

/// <summary>
/// Helpers for rewriting markdown citations and rendering markdown lines.
/// Mirrors codex-rs/tui/src/markdown.rs (citation rewrite and append_markdown done).
/// </summary>

namespace CodexCli.Util;

public static class MarkdownUtils
{

    public static string RewriteFileCitations(string src, UriBasedFileOpener opener, string cwd)
    {
        var scheme = opener.GetScheme();
        if (scheme == null) return src;
        return CitationRegex.Instance.Replace(src, m =>
        {
            var file = m.Groups[1].Value;
            var line = m.Groups[2].Value;
            var path = Path.IsPathRooted(file) ? Path.GetFullPath(file) : Path.GetFullPath(Path.Combine(cwd, file));
            path = path.Replace("\\", "/");
            return $"[{file}:{line}]({scheme}://file{path}:{line}) ";
        });
    }

    public static void AppendMarkdown(string markdown, IList<string> lines, UriBasedFileOpener opener, string cwd)
    {
        var processed = RewriteFileCitations(markdown, opener, cwd);
        foreach (var line in processed.Split('\n'))
            lines.Add(line);
    }
}

