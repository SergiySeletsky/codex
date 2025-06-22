// Rust analog: codex-rs/core/src/codex.rs (partial)
// Ported from codex-rs/core/src/codex.rs spawn interface (partial)
using CodexCli.Protocol;
using CodexCli.Models;
using CodexCli.Config;
using CodexCli.ApplyPatch;
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

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `get_last_assistant_message_from_turn` (done).
    /// </summary>
    public static string? GetLastAssistantMessageFromTurn(List<ResponseItem> responses)
    {
        for (int i = responses.Count - 1; i >= 0; i--)
        {
            if (responses[i] is MessageItem mi && mi.Role == "assistant")
            {
                for (int j = mi.Content.Count - 1; j >= 0; j--)
                {
                    var ci = mi.Content[j];
                    if (ci.Type == "output_text")
                        return ci.Text;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `record_conversation_history` (done).
    /// </summary>
    public static bool RecordConversationHistory(bool disableResponseStorage, WireApi wireApi)
    {
        if (disableResponseStorage)
            return true;
        return wireApi == WireApi.Chat;
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `convert_apply_patch_to_protocol` (done).
    /// </summary>
    public static Dictionary<string, FileChange> ConvertApplyPatchToProtocol(ApplyPatchAction action)
    {
        var result = new Dictionary<string, FileChange>(action.Changes.Count);
        foreach (var kv in action.Changes)
        {
            var c = kv.Value;
            FileChange fc = c.Kind switch
            {
                "add" => new AddFileChange(c.Content ?? string.Empty),
                "delete" => new DeleteFileChange(),
                "update" => new UpdateFileChange(c.UnifiedDiff ?? string.Empty, c.MovePath),
                _ => throw new InvalidOperationException($"unknown change kind {c.Kind}")
            };
            result[kv.Key] = fc;
        }
        return result;
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `first_offending_path` (done).
    /// Returns the first path in the apply_patch action that is not under any
    /// writable root. Paths may be relative to <paramref name="cwd"/>.
    /// </summary>
    public static string? FirstOffendingPath(ApplyPatchAction action, List<string> writableRoots, string cwd)
    {
        foreach (var kv in action.Changes)
        {
            var change = kv.Value;
            var candidate = change.Kind switch
            {
                "add" => kv.Key,
                "delete" => kv.Key,
                "update" => change.MovePath ?? kv.Key,
                _ => kv.Key
            };

            var abs = Path.GetFullPath(Path.IsPathRooted(candidate) ? candidate : Path.Combine(cwd, candidate));
            bool allowed = false;
            foreach (var root in writableRoots)
            {
                var rootAbs = Path.GetFullPath(Path.IsPathRooted(root) ? root : Path.Combine(cwd, root));
                if (abs.StartsWith(rootAbs))
                {
                    allowed = true;
                    break;
                }
            }

            if (!allowed)
                return candidate;
        }

        return null;
    }
}
