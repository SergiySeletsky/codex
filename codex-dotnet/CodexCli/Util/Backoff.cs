// Ported from codex-rs/core/src/util.rs (done)
namespace CodexCli.Util;

public static class Backoff
{
    private const double BackoffFactor = 1.3;
    private const int InitialDelayMs = 200;

    public static TimeSpan GetDelay(int attempt)
    {
        var exp = Math.Pow(BackoffFactor, Math.Max(0, attempt - 1));
        var baseMs = InitialDelayMs * exp;
        var jitter = new Random().NextDouble() * 0.2 + 0.9; // 0.9..1.1
        return TimeSpan.FromMilliseconds(baseMs * jitter);
    }
}
