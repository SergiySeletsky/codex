using System;
using System.Net;

namespace CodexCli.Util;

// Ported from codex-rs/core/src/error.rs (done)
public enum SandboxErr
{
    Denied,
    SeccompInstall,
    SeccompBackend,
    Timeout,
    Signal,
    LandlockRestrict,
}

public class EnvVarError : Exception
{
    public string Var { get; }
    public string? Instructions { get; }

    public EnvVarError(string var, string? instructions = null)
        : base($"Missing environment variable: `{var}`." + (instructions != null ? $" {instructions}" : string.Empty))
    {
        Var = var;
        Instructions = instructions;
    }
}

public class CodexException : Exception
{
    private CodexException(string message) : base(message) { }
    private CodexException(string message, Exception inner) : base(message, inner) { }

    public static CodexException Stream(string msg) => new($"stream disconnected before completion: {msg}");
    public static CodexException Timeout() => new("timeout waiting for child process to exit");
    public static CodexException Spawn() => new("spawn failed: child stdout/stderr not captured");
    public static CodexException Interrupted() => new("interrupted (Ctrl-C)");
    public static CodexException UnexpectedStatus(HttpStatusCode status, string body) => new($"unexpected status {status}: {body}");
    public static CodexException RetryLimit(HttpStatusCode status) => new($"exceeded retry limit, last status: {status}");
    public static CodexException InternalAgentDied() => new("internal error; agent loop died unexpectedly");
    public static CodexException Sandbox(SandboxErr err) => new($"sandbox error: {err}");
    public static CodexException LandlockSandboxExecutableNotProvided() => new("codex-linux-sandbox was required but not provided");

    public static CodexException Io(Exception inner) => new("io error", inner);
    public static CodexException Reqwest(Exception inner) => new("http error", inner);
    public static CodexException Json(Exception inner) => new("json error", inner);
    public static CodexException EnvVar(EnvVarError err) => new(err.Message, err);
}
