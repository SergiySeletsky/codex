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
}
