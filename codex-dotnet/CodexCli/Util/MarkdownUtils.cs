using System.Text.RegularExpressions;
using CodexCli.Config;

namespace CodexCli.Util;

public static class MarkdownUtils
{
    private static readonly Regex CitationRegex = new("【F:([^†]+)†L(\\d+)(?:-L(\\d+|\\?))?】", RegexOptions.Compiled);

    public static string RewriteFileCitations(string src, UriBasedFileOpener opener, string cwd)
    {
        var scheme = opener.GetScheme();
        if (scheme == null) return src;
        return CitationRegex.Replace(src, m =>
        {
            var file = m.Groups[1].Value;
            var line = m.Groups[2].Value;
            var path = Path.IsPathRooted(file) ? Path.GetFullPath(file) : Path.GetFullPath(Path.Combine(cwd, file));
            path = path.Replace("\\", "/");
            return $"[{file}:{line}]({scheme}://file{path}:{line}) ";
        });
    }
}
