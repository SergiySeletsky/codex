// Rust analog: codex-rs/core/src/codex_wrapper.rs (done)
using CodexCli.Protocol;
using System.Collections.Generic;
using System.Threading;

namespace CodexCli.Util;

public static class CodexWrapper
{
    public static async Task<(IAsyncEnumerable<Event> Stream, SessionConfiguredEvent SessionEvent, CancellationTokenSource CtrlC)>
        InitCodexAsync(string prompt, OpenAIClient client, string model,
            Func<string, OpenAIClient, string, CancellationToken, IAsyncEnumerable<Event>>? agent = null,
            IReadOnlyList<string>? notifyCommand = null)
    {
        var cts = new CancellationTokenSource();
        var events = agent != null
            ? agent(prompt, client, model, cts.Token)
            : RealCodexAgent.RunAsync(prompt, client, model, null, Array.Empty<string>(), notifyCommand, cts.Token);

        var enumerator = events.GetAsyncEnumerator(cts.Token);
        if (!await enumerator.MoveNextAsync())
            throw new InvalidOperationException("expected SessionConfigured event");
        if (enumerator.Current is not SessionConfiguredEvent sc)
            throw new InvalidOperationException($"expected SessionConfigured but got {enumerator.Current.GetType().Name}");

        async IAsyncEnumerable<Event> Remainder([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancel = default)
        {
            while (await enumerator.MoveNextAsync())
                yield return enumerator.Current;
        }

        return (Remainder(cts.Token), sc, cts);
    }
}
