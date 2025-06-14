using System.Diagnostics;
using System.Text.Json;
using System.Collections.Concurrent;
using CodexCli.Protocol;

namespace CodexCli.Util;

public class McpClient : IDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _stdin;
    private readonly StreamReader _stdout;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonRpcMessage>> _pending = new();
    private int _nextId = 1;
    private readonly CancellationTokenSource _cts = new();

    private McpClient(Process process)
    {
        _process = process;
        _stdin = process.StandardInput;
        _stdout = process.StandardOutput;
        Task.Run(ReaderLoop);
    }

    private static readonly string[] DefaultEnvVars = OperatingSystem.IsWindows() ?
        new[] { "PATH", "PATHEXT", "USERNAME", "USERDOMAIN", "USERPROFILE", "TEMP", "TMP" } :
        new[] { "HOME", "LOGNAME", "PATH", "SHELL", "USER", "__CF_USER_TEXT_ENCODING", "LANG", "LC_ALL", "TERM", "TMPDIR", "TZ" };

    private static IDictionary<string, string> CreateServerEnv(IDictionary<string, string>? extra)
    {
        var env = new Dictionary<string, string>();
        foreach (var var in DefaultEnvVars)
        {
            var val = Environment.GetEnvironmentVariable(var);
            if (val != null) env[var] = val;
        }
        if (extra != null)
        {
            foreach (var kv in extra)
                env[kv.Key] = kv.Value;
        }
        return env;
    }

    public static async Task<McpClient> StartAsync(string program, IEnumerable<string> args, IDictionary<string, string>? env = null)
    {
        var psi = new ProcessStartInfo(program)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            UseShellExecute = false
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        psi.Environment.Clear();
        foreach (var kv in CreateServerEnv(env)) psi.Environment[kv.Key] = kv.Value;
        var p = Process.Start(psi) ?? throw new InvalidOperationException("failed to start process");
        return new McpClient(p);
    }

    private async Task ReaderLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            var line = await _stdout.ReadLineAsync();
            if (line == null) break;
            try
            {
                var msg = JsonSerializer.Deserialize<JsonRpcMessage>(line);
                if (msg?.Id is JsonElement idElem && idElem.ValueKind == JsonValueKind.Number && idElem.TryGetInt32(out var id))
                {
                    if (_pending.TryRemove(id, out var tcs))
                        tcs.TrySetResult(msg);
                }
            }
            catch
            {
                // ignore malformed lines
            }
        }
    }

    public async Task<JsonRpcMessage> SendRequestAsync(string method, object? parameters, int timeoutSeconds = 10)
    {
        int id = Interlocked.Increment(ref _nextId);
        var msg = new JsonRpcMessage
        {
            Method = method,
            Params = parameters != null ? JsonSerializer.SerializeToElement(parameters) : null,
            Id = JsonSerializer.SerializeToElement(id)
        };
        var json = JsonSerializer.Serialize(msg);
        await _stdin.WriteLineAsync(json);
        await _stdin.FlushAsync();
        var tcs = new TaskCompletionSource<JsonRpcMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = tcs;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        await using var reg = cts.Token.Register(() => tcs.TrySetCanceled());
        return await tcs.Task;
    }

    public Task SendNotificationAsync(string method, object? parameters)
    {
        var msg = new JsonRpcMessage
        {
            Method = method,
            Params = parameters != null ? JsonSerializer.SerializeToElement(parameters) : null
        };
        var json = JsonSerializer.Serialize(msg);
        return _stdin.WriteLineAsync(json);
    }

    public async Task<T> SendRequestAsync<T>(string method, object? parameters, int timeoutSeconds = 10)
    {
        var msg = await SendRequestAsync(method, parameters, timeoutSeconds);
        if (msg.Result.HasValue)
        {
            return msg.Result.Value.Deserialize<T>();
        }
        throw new InvalidOperationException("response missing result");
    }

    public Task InitializeAsync(InitializeRequestParams p, int timeoutSeconds = 10)
        => SendRequestAsync("initialize", p, timeoutSeconds);

    public Task<ListToolsResult> ListToolsAsync(ListToolsRequestParams? p = null, int timeoutSeconds = 10)
        => SendRequestAsync<ListToolsResult>("tools/list", p, timeoutSeconds);

    public Task<CallToolResult> CallToolAsync(string name, JsonElement? arguments = null, int timeoutSeconds = 10)
    {
        var p = new CallToolRequestParams(name, arguments);
        return SendRequestAsync<CallToolResult>("tools/call", p, timeoutSeconds);
    }

    public Task PingAsync(int timeoutSeconds = 10)
        => SendRequestAsync("ping", null, timeoutSeconds);

    public void Dispose()
    {
        _cts.Cancel();
        try { if (!_process.HasExited) _process.Kill(); } catch { }
        _process.Dispose();
    }
}

public record ClientCapabilities(object? Experimental, object? Roots, object? Sampling);
public record Implementation(string Name, string Version);
public record InitializeRequestParams(ClientCapabilities Capabilities, Implementation ClientInfo, string ProtocolVersion);
public record ListToolsRequestParams(string? Cursor);
public record ToolInputSchema(JsonElement? Properties, List<string>? Required, string Type);
public record ToolAnnotations(bool? DestructiveHint, bool? IdempotentHint, bool? OpenWorldHint, bool? ReadOnlyHint, string? Title);
public record Tool(string Name, ToolInputSchema InputSchema, string? Description, ToolAnnotations? Annotations);
public record ListToolsResult(string? NextCursor, List<Tool> Tools);
public record CallToolRequestParams(string Name, JsonElement? Arguments);
public record CallToolResult(List<JsonElement> Content, bool? IsError);

