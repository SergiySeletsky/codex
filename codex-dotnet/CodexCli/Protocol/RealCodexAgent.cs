using CodexCli.Util;
using CodexCli.Models;
using CodexCli.Config;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// Ported from codex-rs/core/src/client.rs (streaming model client with Ctrl+C parity).
/// RunWithRolloutAsync parity tested in RealCodexAgentRolloutTests and used by
/// ExecCommand for rollout persistence.

namespace CodexCli.Protocol;

public static class RealCodexAgent
{
    private record SseEvent(string Type, System.Text.Json.JsonElement? Response, System.Text.Json.JsonElement? Item);

    // Used by RealCodexAgentSseFixtureTests and CrossCliCompatTests.ExecSseFixtureMatches for parity with Rust stream_from_fixture
    private static async IAsyncEnumerable<ResponseEvent> StreamFromFixture(string path)
    {
        using var reader = new StreamReader(path);
        string? line;
        string? eventType = null;
        string? data = null;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                if (eventType != null && data != null)
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(data);
                    var root = doc.RootElement;
                    var sse = new SseEvent(eventType,
                        root.TryGetProperty("response", out var r) ? r : null,
                        root.TryGetProperty("item", out var i) ? i : null);
                    if (sse.Type == "response.output_item.done" && sse.Item.HasValue)
                    {
                        var item = System.Text.Json.JsonSerializer.Deserialize<ResponseItem>(sse.Item.Value.GetRawText());
                        if (item != null)
                            yield return new OutputItemDone(item);
                    }
                    else if (sse.Type == "response.completed" && sse.Response.HasValue)
                    {
                        var id = sse.Response.Value.GetProperty("id").GetString() ?? string.Empty;
                        yield return new Completed(id);
                    }
                }
                eventType = null;
                data = null;
                continue;
            }
            if (line.StartsWith("event:"))
                eventType = line.Substring(6).Trim();
            else if (line.StartsWith("data:"))
                data = line.Substring(5).Trim();
        }
    }
    public static async IAsyncEnumerable<Event> RunAsync(
        string prompt,
        OpenAIClient client,
        string model,
        Func<Event, Task<ReviewDecision>>? approvalResponder = null,
        IReadOnlyList<string>? images = null,
        IReadOnlyList<string>? notifyCommand = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancel = default)
    {
        yield return new SessionConfiguredEvent(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), model);
        var subId = Guid.NewGuid().ToString();
        yield return new TaskStartedEvent(subId);

        async IAsyncEnumerable<ResponseEvent> Stream()
        {
            if (EnvFlags.CODEX_RS_SSE_FIXTURE is { } fixture)
            {
                await foreach (var ev in StreamFromFixture(fixture))
                    yield return ev;
            }
            else
            {
                await foreach (var chunk in client.ChatStreamAsync(prompt, cancel).WithCancellation(cancel))
                {
                    yield return new OutputItemDone(new MessageItem("assistant", new List<ContentItem>{ new("output_text", chunk) }));
                }
                yield return new Completed(Guid.NewGuid().ToString());
            }
        }

        var full = new System.Text.StringBuilder();
        var msgId = Guid.NewGuid().ToString();
        bool interrupted = false;
        await foreach (var ev in Stream())
        {
            if (cancel.IsCancellationRequested)
            {
                interrupted = true;
                break;
            }
            switch (ev)
            {
                case OutputItemDone { Item: MessageItem msg }:
                    var text = msg.Content.Count > 0 ? msg.Content[0].Text : string.Empty;
                    full.Append(text);
                    yield return new AgentMessageEvent(msgId, text);
                    break;
                case Completed c:
                    if (!interrupted)
                    {
                        var complete = new TaskCompleteEvent(subId, full.ToString());
                        yield return complete;
                        Codex.MaybeNotify(notifyCommand?.ToList(), new AgentTurnCompleteNotification(complete.Id, new[] { prompt }, complete.LastAgentMessage));
                    }
                    break;
            }
        }
        if (interrupted || cancel.IsCancellationRequested)
            yield return new ErrorEvent(Guid.NewGuid().ToString(), "Interrupted");
    }

    /// <summary>
    /// Wrapper over <see cref="RunAsync"/> that records streamed events to a rollout file.
    /// </summary>
    public static async IAsyncEnumerable<Event> RunWithRolloutAsync(
        string prompt,
        OpenAIClient client,
        string model,
        RolloutRecorder recorder,
        Func<Event, Task<ReviewDecision>>? approvalResponder = null,
        IReadOnlyList<string>? images = null,
        IReadOnlyList<string>? notifyCommand = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancel = default)
    {
        await foreach (var ev in RunAsync(prompt, client, model, approvalResponder, images, notifyCommand, cancel))
        {
            if (ResponseItemFactory.FromEvent(ev) is { } ri)
                await Codex.RecordRolloutItemsAsync(recorder, new[] { ri });
            yield return ev;
        }
    }
}
