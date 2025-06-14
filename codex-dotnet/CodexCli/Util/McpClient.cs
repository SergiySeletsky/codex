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
        if (env != null)
        {
            psi.Environment.Clear();
            foreach (var kv in env) psi.Environment[kv.Key] = kv.Value;
        }
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

