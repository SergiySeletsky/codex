namespace CodexCli.Config;

public enum ShellEnvironmentPolicyInherit
{
    Core,
    All,
    None
}

public class EnvironmentVariablePattern
{
    private readonly string _pattern;
    private readonly System.Text.RegularExpressions.Regex _regex;

    public EnvironmentVariablePattern(string pattern)
    {
        _pattern = pattern;
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        _regex = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    public static EnvironmentVariablePattern CaseInsensitive(string pattern) => new(pattern);

    public bool Matches(string name) => _regex.IsMatch(name);
}

public class ShellEnvironmentPolicy
{
    public ShellEnvironmentPolicyInherit Inherit { get; set; } = ShellEnvironmentPolicyInherit.Core;
    public bool IgnoreDefaultExcludes { get; set; }
    public List<EnvironmentVariablePattern> Exclude { get; set; } = new();
    public Dictionary<string,string> Set { get; set; } = new();
    public List<EnvironmentVariablePattern> IncludeOnly { get; set; } = new();
}
