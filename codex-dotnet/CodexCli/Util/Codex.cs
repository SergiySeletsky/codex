// Rust analog: codex-rs/core/src/codex.rs (partial)
// Ported from codex-rs/core/src/codex.rs spawn interface (partial)
using CodexCli.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace CodexCli.Util;

public class Codex
{
    private readonly IAsyncEnumerator<Event> _enumerator;
    private readonly CancellationTokenSource _ctrlC;

    private Codex(IAsyncEnumerator<Event> enumerator, CancellationTokenSource ctrlC)
    {
        _enumerator = enumerator;
        _ctrlC = ctrlC;
    }

    public static async Task<(Codex Codex, string SessionId)> SpawnAsync(
        string prompt,
        OpenAIClient client,
        string model,
        Func<string, OpenAIClient, string, CancellationToken, IAsyncEnumerable<Event>>? agent = null)
    {
        var (stream, sc, cts) = await CodexWrapper.InitCodexAsync(prompt, client, model, agent);
        return (new Codex(stream.GetAsyncEnumerator(cts.Token), cts), sc.Id);
    }

    public async Task<Event?> NextEventAsync()
    {
        if (await _enumerator.MoveNextAsync())
            return _enumerator.Current;
        return null;
    }

    public void Cancel() => _ctrlC.Cancel();

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `format_exec_output` (done).
    /// </summary>
    public static string FormatExecOutput(string output, int exitCode, TimeSpan duration)
    {
        var payload = new
        {
            output,
            metadata = new
            {
                exit_code = exitCode,
                duration_seconds = Math.Round(duration.TotalSeconds * 10) / 10
            }
        };
        return JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `get_writable_roots` (done).
    /// </summary>
    public static List<string> GetWritableRoots(string cwd)
    {
        var roots = new List<string>();
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
        {
            roots.Add(Path.GetTempPath());
            var home = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(home))
                roots.Add(Path.Combine(home, ".pyenv"));
        }
        roots.Add(cwd);
        return roots;
    }
}
