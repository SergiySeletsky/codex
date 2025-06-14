namespace CodexCli.Util;

public static class TokenManager
{
    private static readonly string TokenFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "token");

    public static void SaveToken(string token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TokenFile)!);
        File.WriteAllText(TokenFile, token.Trim());
    }

    public static string? LoadToken()
    {
        return File.Exists(TokenFile) ? File.ReadAllText(TokenFile).Trim() : null;
    }
}
