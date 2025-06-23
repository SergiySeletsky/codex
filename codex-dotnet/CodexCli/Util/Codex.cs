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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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
    /// Ported from codex-rs/core/src/codex.rs `resolve_path` (done).
    /// Joins <paramref name="path"/> to <paramref name="cwd"/> if provided, otherwise returns <paramref name="cwd"/>.
    /// </summary>
    public static string ResolvePath(string cwd, string? path)
    {
        if (string.IsNullOrEmpty(path))
            return cwd;
        return Path.IsPathRooted(path) ? path : Path.Combine(cwd, path);
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

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `to_exec_params` (done).
    /// Converts shell tool call parameters to ExecParams using the provided
    /// environment policy and working directory.
    /// </summary>
    public static ExecParams ToExecParams(ShellToolCallParams p, ShellEnvironmentPolicy policy, string cwd)
    {
        var workdir = p.Workdir != null
            ? (Path.IsPathRooted(p.Workdir) ? p.Workdir : Path.Combine(cwd, p.Workdir))
            : cwd;
        workdir = Path.GetFullPath(workdir);
        return new ExecParams(p.Command, workdir, p.TimeoutMs, ExecEnv.Create(policy));
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `parse_container_exec_arguments` (done).
    /// Attempts to parse function call arguments into ExecParams. On failure
    /// returns a FunctionCallOutputInputItem mirroring the Rust error pathway.
    /// </summary>
    public static bool TryParseContainerExecArguments(string arguments, ShellEnvironmentPolicy policy, string cwd, string callId, out ExecParams? execParams, out ResponseInputItem? error)
    {
        try
        {
            var shellParams = JsonSerializer.Deserialize<ShellToolCallParams>(arguments);
            if (shellParams == null)
                throw new JsonException("null parameters");
            execParams = ToExecParams(shellParams, policy, cwd);
            error = null;
            return true;
        }
        catch (Exception e)
        {
            execParams = null;
            error = new FunctionCallOutputInputItem(callId, new FunctionCallOutputPayload($"failed to parse function arguments: {e.Message}", null));
            return false;
        }
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `maybe_notify` (done).
    /// Spawns the configured notifier with the serialized notification payload.
    /// </summary>
    public static void MaybeNotify(List<string>? notifyCommand, UserNotification notification)
    {
        if (notifyCommand == null || notifyCommand.Count == 0)
            return;

        string json;
        try
        {
            json = JsonSerializer.Serialize(notification);
        }
        catch (Exception)
        {
            Console.Error.WriteLine("failed to serialise notification payload");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo(notifyCommand[0]) { UseShellExecute = false };
            for (int i = 1; i < notifyCommand.Count; i++)
                psi.ArgumentList.Add(notifyCommand[i]);
            psi.ArgumentList.Add(json);
            Process.Start(psi);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"failed to spawn notifier '{notifyCommand[0]}': {e.Message}");
        }
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `inject_input` (done).
    /// Adds user input to the pending queue if a task is running.
    /// Returns true if the input was queued, otherwise false.
    /// </summary>
    public static bool InjectInput(CodexState state, List<InputItem> input)
    {
        if (state.HasCurrentTask)
        {
            state.PendingInput.Add(InputItem.ToResponse(input));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `get_pending_input` (done).
    /// Returns any queued input and clears the pending list.
    /// </summary>
    public static List<ResponseInputItem> GetPendingInput(CodexState state)
    {
        var ret = new List<ResponseInputItem>(state.PendingInput);
        state.PendingInput.Clear();
        return ret;
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `call_tool` (done).
    /// Delegates to <see cref="McpConnectionManager.CallToolAsync"/> using server and tool names.
    /// </summary>
    public static Task<CallToolResult> CallToolAsync(
        McpConnectionManager manager,
        string server,
        string tool,
        JsonElement? arguments = null,
        TimeSpan? timeout = null)
    {
        var fq = McpConnectionManager.FullyQualifiedToolName(server, tool);
        return manager.CallToolAsync(fq, arguments, timeout);
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `notify_exec_command_begin` (done).
    /// Creates an ExecCommandBeginEvent for the given parameters.
    /// </summary>
    public static ExecCommandBeginEvent NotifyExecCommandBegin(string subId, string callId, ExecParams parameters)
    {
        return new ExecCommandBeginEvent(subId, parameters.Command, parameters.Cwd);
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `notify_exec_command_end` (done).
    /// Creates an ExecCommandEndEvent with truncated output streams.
    /// </summary>
    public static ExecCommandEndEvent NotifyExecCommandEnd(string subId, string callId, string stdout, string stderr, int exitCode)
    {
        const int MaxStreamOutput = 5 * 1024;
        var outTrunc = stdout.Length > MaxStreamOutput ? stdout[..MaxStreamOutput] : stdout;
        var errTrunc = stderr.Length > MaxStreamOutput ? stderr[..MaxStreamOutput] : stderr;
        return new ExecCommandEndEvent(subId, outTrunc, errTrunc, exitCode);
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `notify_background_event` (done).
    /// Creates a BackgroundEvent with the provided message.
    /// </summary>
    public static BackgroundEvent NotifyBackgroundEvent(string subId, string message)
    {
        return new BackgroundEvent(subId, message);
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `set_task` (done).
    /// Replaces any current task with the provided one, aborting the old task.
    /// </summary>
    public static void SetTask(CodexState state, AgentTask task)
    {
        state.CurrentTask?.Abort();
        state.CurrentTask = task;
        state.HasCurrentTask = true;
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `remove_task` (done).
    /// Removes the task if its subId matches.
    /// </summary>
    public static void RemoveTask(CodexState state, string subId)
    {
        if (state.CurrentTask != null && state.CurrentTask.SubId == subId)
        {
            state.CurrentTask = null;
            state.HasCurrentTask = false;
        }
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `record_rollout_items` (done).
    /// Appends the items to the rollout recorder if it is not null.
    /// </summary>
    public static async Task RecordRolloutItemsAsync(RolloutRecorder? recorder, IEnumerable<ResponseItem> items)
    {
        if (recorder == null)
            return;

        try
        {
            await recorder.RecordItemsAsync(items);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"failed to record rollout items: {e.Message}");
        }
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `record_conversation_items` (done).
    /// Records items to the rollout and conversation transcript if provided.
    /// </summary>
    public static async Task RecordConversationItemsAsync(RolloutRecorder? recorder, ConversationHistory? transcript, IEnumerable<ResponseItem> items)
    {
        await RecordRolloutItemsAsync(recorder, items);
        transcript?.RecordItems(items);
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `request_command_approval` (done).
    /// Returns a task that completes with the user's decision and the approval event.
    /// </summary>
    public static (Task<ReviewDecision> Task, ExecApprovalRequestEvent Event) RequestCommandApproval(
        CodexState state, string subId, List<string> command, string cwd, string? reason)
    {
        var tcs = new TaskCompletionSource<ReviewDecision>();
        state.PendingApprovals[subId] = tcs;
        var ev = new ExecApprovalRequestEvent(subId, command);
        return (tcs.Task, ev);
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `request_patch_approval` (done).
    /// </summary>
    public static (Task<ReviewDecision> Task, PatchApplyApprovalRequestEvent Event) RequestPatchApproval(
        CodexState state, string subId, ApplyPatchAction action, string? reason, string? grantRoot)
    {
        var tcs = new TaskCompletionSource<ReviewDecision>();
        state.PendingApprovals[subId] = tcs;
        var summary = string.Join(", ", action.Changes.Keys);
        var ev = new PatchApplyApprovalRequestEvent(subId, summary);
        return (tcs.Task, ev);
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `notify_approval` (done).
    /// Completes the pending approval task if present.
    /// </summary>
    public static void NotifyApproval(CodexState state, string subId, ReviewDecision decision)
    {
        if (state.PendingApprovals.TryGetValue(subId, out var tcs))
        {
            state.PendingApprovals.Remove(subId);
            tcs.TrySetResult(decision);
        }
    }

    /// <summary>
    /// Ported from codex-rs/core/src/codex.rs `add_approved_command` (done).
    /// </summary>
    public static void AddApprovedCommand(CodexState state, List<string> command)
    {
        state.ApprovedCommands.Add(command);
    }
}
