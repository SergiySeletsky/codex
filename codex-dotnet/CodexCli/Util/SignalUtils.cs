// Ported from codex-rs/core/src/util.rs (done)
namespace CodexCli.Util;

public static class SignalUtils
{
    public static CancellationTokenSource NotifyOnSigInt()
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        return cts;
    }

    public static CancellationTokenSource NotifyOnSigTerm()
    {
        var cts = new CancellationTokenSource();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => cts.Cancel();
        return cts;
    }
}
