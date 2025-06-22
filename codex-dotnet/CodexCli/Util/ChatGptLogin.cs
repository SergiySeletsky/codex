// Ported from codex-rs/login/src/lib.rs (done)
using System.Diagnostics;
using System.ComponentModel;
using System.Text.Json;

namespace CodexCli.Util;

public static class ChatGptLogin
{
    private static string ScriptPath => Path.Combine(AppContext.BaseDirectory, "Login", "login_with_chatgpt.py");

    public static string GetScriptPath() => ScriptPath;

    public static async Task<string> LoginAsync(string codexHome, bool captureOutput, IDictionary<string,string>? env = null)
    {
        var psi = new ProcessStartInfo("python3", ScriptPath)
        {
            RedirectStandardOutput = captureOutput,
            RedirectStandardError = captureOutput,
            UseShellExecute = false,
        };
        psi.Environment["CODEX_HOME"] = codexHome;
        if (env != null)
        {
            foreach (var (k,v) in env)
                psi.Environment[k] = v;
        }
        try
        {
            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("python3 not found");
            var stdout = captureOutput ? await proc.StandardOutput.ReadToEndAsync() : null;
            var stderr = captureOutput ? await proc.StandardError.ReadToEndAsync() : null;
            await proc.WaitForExitAsync();
            if (proc.ExitCode != 0)
                throw new InvalidOperationException($"login_with_chatgpt failed: {stderr}");
        }
        catch (Exception e) when (e is Win32Exception or FileNotFoundException)
        {
            throw new InvalidOperationException("Failed to run login_with_chatgpt", e);
        }
        var authPath = Path.Combine(codexHome, "auth.json");
        var json = await File.ReadAllTextAsync(authPath);
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("OPENAI_API_KEY").GetString() ?? string.Empty;
    }
}
