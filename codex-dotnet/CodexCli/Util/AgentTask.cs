// Ported from codex-rs/core/src/codex.rs AgentTask (spawn logic simplified)
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CodexCli.Protocol;

namespace CodexCli.Util;

/// <summary>
/// Simple representation of a running agent task used by Codex.SetTask.
/// Ported from codex-rs/core/src/codex.rs `AgentTask` (partial).
/// </summary>
public class AgentTask
{
    public string SubId { get; }
    private readonly Action _abort;
    public bool Aborted { get; private set; }
    public Task? RunningTask { get; }

    public AgentTask(string subId, Action abort, Task? runningTask = null)
    {
        SubId = subId;
        _abort = abort;
        RunningTask = runningTask;
    }

    public void Abort()
    {
        if (Aborted) return;
        Aborted = true;
        _abort?.Invoke();
    }

    /// <summary>
    /// Simplified port of codex-rs/core/src/codex.rs `AgentTask::spawn` (done).
    /// Runs <paramref name="events"/> in the background and forwards them to
    /// <paramref name="writer"/>.
    /// </summary>
    public static AgentTask Spawn(
        ChannelWriter<Event> writer,
        string subId,
        IAsyncEnumerable<Event> events,
        CancellationToken cancel = default)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancel);
        var running = Task.Run(async () =>
        {
            await writer.WriteAsync(new TaskStartedEvent(subId));
            string? lastMessage = null;
            try
            {
                await foreach (var ev in events.WithCancellation(cts.Token))
                {
                    if (ev is AgentMessageEvent msg)
                        lastMessage = msg.Message;
                    await writer.WriteAsync(ev);
                }
                await writer.WriteAsync(new TaskCompleteEvent(subId, lastMessage));
            }
            catch (OperationCanceledException)
            {
                // interrupted
            }
        }, cts.Token);

        void Abort()
        {
            if (!cts.IsCancellationRequested)
            {
                cts.Cancel();
                writer.TryWrite(new ErrorEvent(subId, "Turn interrupted"));
            }
        }

        return new AgentTask(subId, Abort, running);
    }
}
