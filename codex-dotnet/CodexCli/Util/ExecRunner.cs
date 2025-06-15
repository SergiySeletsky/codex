using CodexCli.Models;
using CodexCli.Protocol;

namespace CodexCli.Util;

public static class ExecRunner
{
    private const int MaxOutputBytes = 10 * 1024;
    private const string NetworkDisabledEnv = "CODEX_SANDBOX_NETWORK_DISABLED";
    private const int MaxOutputLines = 256;

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

        if (policy != null && !policy.HasFullNetworkAccess())
            psi.Environment[NetworkDisabledEnv] = "1";
        using var proc = System.Diagnostics.Process.Start(psi)!;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (p.TimeoutMs != null)
            cts.CancelAfter(p.TimeoutMs.Value);
        var start = DateTime.UtcNow;
        var stdoutTask = ReadCappedAsync(proc.StandardOutput, cts.Token);
        var stderrTask = ReadCappedAsync(proc.StandardError, cts.Token);
        await proc.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return new ExecToolCallOutput(proc.ExitCode, stdout, stderr, DateTime.UtcNow-start);
    }

    private static async Task<string> ReadCappedAsync(StreamReader reader, CancellationToken token)
    {
        var sb = new System.Text.StringBuilder();
        int lines = 0;
        char[] buf = new char[1024];
        while (!reader.EndOfStream && sb.Length < MaxOutputBytes && lines < MaxOutputLines)
        {
            int n = await reader.ReadAsync(buf.AsMemory(0, Math.Min(buf.Length, MaxOutputBytes - sb.Length)), token);
            if (n == 0) break;
            for (int i=0;i<n;i++) if (buf[i]=='\n') lines++; 
            sb.Append(buf,0,n);
        }
        return sb.ToString();
    }
}
