using CodexCli.Models;
using CodexCli.Protocol;

namespace CodexCli.Util;

/// <summary>
/// Simplified port of codex-rs/core/src/exec.rs (done).
/// Cross-CLI parity covered in CrossCliCompatTests.ExecJsonMatches,
/// ExecPatchSummaryMatches, ExecCancelImmediatelyMatches and
/// ExecNetworkEnvMatches.
/// </summary>

public static class ExecRunner
{
    public const string NetworkDisabledEnv = "CODEX_SANDBOX_NETWORK_DISABLED";
    public const string SessionEnv = "CODEX_SESSION_ID";
    public const int DefaultMaxOutputBytes = 10 * 1024;
    public const int DefaultMaxOutputLines = 256;

    public static async Task<ExecToolCallOutput> RunAsync(ExecParams p, CancellationToken token, SandboxPolicy? policy = null)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(p.Command[0])
        {
            WorkingDirectory = p.Cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false
        };
        for (int i = 1; i < p.Command.Count; i++)
            psi.ArgumentList.Add(p.Command[i]);
        foreach (var (k,v) in p.Env)
            psi.Environment[k] = v;
        if (p.SessionId != null)
            psi.Environment[SessionEnv] = p.SessionId;

        if (policy != null && !policy.HasFullNetworkAccess())
            psi.Environment[NetworkDisabledEnv] = "1";
        using var proc = System.Diagnostics.Process.Start(psi)!;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (p.TimeoutMs != null)
            cts.CancelAfter(p.TimeoutMs.Value);
        var start = DateTime.UtcNow;
        var stdoutTask = ReadCappedAsync(proc.StandardOutput, cts.Token, p.MaxOutputBytes ?? DefaultMaxOutputBytes, p.MaxOutputLines ?? DefaultMaxOutputLines);
        var stderrTask = ReadCappedAsync(proc.StandardError, cts.Token, p.MaxOutputBytes ?? DefaultMaxOutputBytes, p.MaxOutputLines ?? DefaultMaxOutputLines);
        try
        {
            await proc.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            throw;
        }
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return new ExecToolCallOutput(proc.ExitCode, stdout, stderr, DateTime.UtcNow-start);
    }

    private static async Task<string> ReadCappedAsync(StreamReader reader, CancellationToken token, int maxBytes, int maxLines)
    {
        var sb = new System.Text.StringBuilder();
        int lines = 0;
        char[] buf = new char[1024];
        while (!reader.EndOfStream && sb.Length < maxBytes && lines < maxLines)
        {
            int n = await reader.ReadAsync(buf.AsMemory(0, Math.Min(buf.Length, maxBytes - sb.Length)), token);
            if (n == 0) break;
            for (int i=0;i<n;i++) if (buf[i]=='\n') lines++; 
            sb.Append(buf,0,n);
        }
        return sb.ToString();
    }
}
