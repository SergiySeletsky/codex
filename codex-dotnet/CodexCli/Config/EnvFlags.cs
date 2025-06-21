using System;
// Mirrors codex-rs/core/src/flags.rs (done)

namespace CodexCli.Config;

/// <summary>
/// Environment flag helpers mirrored from codex-rs/core/src/flags.rs (done).
/// </summary>
public static class EnvFlags
{
    public static string OPENAI_DEFAULT_MODEL =>
        Environment.GetEnvironmentVariable("OPENAI_DEFAULT_MODEL") ?? "codex-mini-latest";

    public static string OPENAI_API_BASE =>
        Environment.GetEnvironmentVariable("OPENAI_API_BASE") ?? "https://api.openai.com/v1";

    public static string? OPENAI_API_KEY =>
        Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    public static TimeSpan OPENAI_TIMEOUT_MS =>
        TimeSpan.FromMilliseconds(
            int.TryParse(Environment.GetEnvironmentVariable("OPENAI_TIMEOUT_MS"), out var ms) ? ms : 300_000);

    public static ulong OPENAI_REQUEST_MAX_RETRIES =>
        ulong.TryParse(Environment.GetEnvironmentVariable("OPENAI_REQUEST_MAX_RETRIES"), out var val) ? val : 4UL;

    public static ulong OPENAI_STREAM_MAX_RETRIES =>
        ulong.TryParse(Environment.GetEnvironmentVariable("OPENAI_STREAM_MAX_RETRIES"), out var val) ? val : 10UL;

    public static TimeSpan OPENAI_STREAM_IDLE_TIMEOUT_MS =>
        TimeSpan.FromMilliseconds(
            int.TryParse(Environment.GetEnvironmentVariable("OPENAI_STREAM_IDLE_TIMEOUT_MS"), out var ms) ? ms : 300_000);

    public static string? CODEX_RS_SSE_FIXTURE =>
        Environment.GetEnvironmentVariable("CODEX_RS_SSE_FIXTURE");
}
