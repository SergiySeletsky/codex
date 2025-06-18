using System.Text.RegularExpressions;

namespace CodexCli.Util;

/// <summary>
/// Regex helper matching Codex-style source file citations like:
/// 【F:src/main.rs†L10-L20】
/// Mirrors codex-rs/tui/src/citation_regex.rs (done).
/// </summary>
public static class CitationRegex
{
    public static readonly Regex Instance = new("【F:([^†]+)†L(\\d+)(?:-L(\\d+|\\?))?】", RegexOptions.Compiled);
}
