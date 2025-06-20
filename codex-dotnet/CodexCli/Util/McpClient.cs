using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using CodexCli.Protocol;

namespace CodexCli.Util;

/// <summary>
/// Port of rust `codex-rs/mcp-client/src/mcp_client.rs` (done; handshake,
/// list-roots, list-tools and ping tested)
/// </summary>

public class McpClient : IDisposable, IAsyncDisposable
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

    public Task<ListRootsResult> ListRootsAsync(int timeoutSeconds = 10)
        => SendRequestAsync<ListRootsResult>("roots/list", null, timeoutSeconds);

    public Task<CallToolResult> CallToolAsync(string name, JsonElement? arguments = null, int timeoutSeconds = 10)
    {
        var p = new CallToolRequestParams(name, arguments);
        return SendRequestAsync<CallToolResult>("tools/call", p, timeoutSeconds);
    }

    public Task PingAsync(int timeoutSeconds = 10)
        => SendRequestAsync("ping", null, timeoutSeconds);

    public Task<ListResourcesResult> ListResourcesAsync(ListResourcesRequestParams? p = null, int timeoutSeconds = 10)
        => SendRequestAsync<ListResourcesResult>("resources/list", p, timeoutSeconds);

    public Task<ListResourceTemplatesResult> ListResourceTemplatesAsync(ListResourceTemplatesRequestParams? p = null, int timeoutSeconds = 10)
        => SendRequestAsync<ListResourceTemplatesResult>("resources/templates/list", p, timeoutSeconds);

    public Task<JsonElement> ReadResourceAsync(ReadResourceRequestParams p, int timeoutSeconds = 10)
        => SendRequestAsync<JsonElement>("resources/read", p, timeoutSeconds);

    public Task<Result> WriteResourceAsync(WriteResourceRequestParams p, int timeoutSeconds = 10)
        => SendRequestAsync<Result>("resources/write", p, timeoutSeconds);

    public Task SubscribeAsync(SubscribeRequestParams p, int timeoutSeconds = 10)
        => SendRequestAsync("resources/subscribe", p, timeoutSeconds);

    public Task UnsubscribeAsync(UnsubscribeRequestParams p, int timeoutSeconds = 10)
        => SendRequestAsync("resources/unsubscribe", p, timeoutSeconds);

    public Task<ListPromptsResult> ListPromptsAsync(ListPromptsRequestParams? p = null, int timeoutSeconds = 10)
        => SendRequestAsync<ListPromptsResult>("prompts/list", p, timeoutSeconds);

    public Task<GetPromptResult> GetPromptAsync(string name, JsonElement? arguments = null, int timeoutSeconds = 10)
    {
        var p = new GetPromptRequestParams(name, arguments);
        return SendRequestAsync<GetPromptResult>("prompts/get", p, timeoutSeconds);
    }

    public Task<Result> AddPromptAsync(AddPromptRequestParams p, int timeoutSeconds = 10)
        => SendRequestAsync<Result>("prompts/add", p, timeoutSeconds);

    public Task<Result> SetLevelAsync(string level, int timeoutSeconds = 10)
        => SendRequestAsync<Result>("logging/setLevel", new SetLevelRequestParams(level), timeoutSeconds);

    public Task<CompleteResult> CompleteAsync(CompleteRequestParams p, int timeoutSeconds = 10)
        => SendRequestAsync<CompleteResult>("completion/complete", p, timeoutSeconds);

    public Task<CreateMessageResult> CreateMessageAsync(CreateMessageRequestParams p, int timeoutSeconds = 10)
        => SendRequestAsync<CreateMessageResult>("sampling/createMessage", p, timeoutSeconds);

    public Task<Result> AddMessageAsync(string text, int timeoutSeconds = 10)
        => SendRequestAsync<Result>("messages/add", new { text }, timeoutSeconds);

    public Task<GetMessageEntryResult> GetMessageEntryAsync(int offset, int timeoutSeconds = 10)
        => SendRequestAsync<GetMessageEntryResult>("messages/getEntry", new { offset }, timeoutSeconds);

    public Task<MessagesResult> ListMessagesAsync(int timeoutSeconds = 10)
        => SendRequestAsync<MessagesResult>("messages/list", null, timeoutSeconds);

    public Task<CountMessagesResult> CountMessagesAsync(int timeoutSeconds = 10)
        => SendRequestAsync<CountMessagesResult>("messages/count", null, timeoutSeconds);

    public Task<Result> ClearMessagesAsync(int timeoutSeconds = 10)
        => SendRequestAsync<Result>("messages/clear", null, timeoutSeconds);

    public Task<MessagesResult> SearchMessagesAsync(string term, int limit = 10, int timeoutSeconds = 10)
        => SendRequestAsync<MessagesResult>("messages/search", new SearchMessagesRequestParams(term, limit), timeoutSeconds);

    public Task<MessagesResult> LastMessagesAsync(int count = 10, int timeoutSeconds = 10)
        => SendRequestAsync<MessagesResult>("messages/last", new LastMessagesRequestParams(count), timeoutSeconds);

    public Task<Result> AddRootAsync(string uri, int timeoutSeconds = 10)
        => SendRequestAsync<Result>("roots/add", new { uri }, timeoutSeconds);

    public Task<Result> RemoveRootAsync(string uri, int timeoutSeconds = 10)
        => SendRequestAsync<Result>("roots/remove", new { uri }, timeoutSeconds);

    public Task<CallToolResult> CallCodexAsync(CodexToolCallParam param, int timeoutSeconds = 10)
    {
        var args = JsonSerializer.SerializeToElement(param);
        var p = new CallToolRequestParams("codex", args);
        return SendRequestAsync<CallToolResult>("tools/call", p, timeoutSeconds);
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { if (!_process.HasExited) _process.Kill(); } catch { }
        _process.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
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

public record Root(string? Name, string Uri);
public record ListRootsResult(List<Root> Roots);
public record CallToolRequestParams(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("arguments")] JsonElement? Arguments);
public record CallToolResult(List<JsonElement> Content, bool? IsError);

public record ListResourcesRequestParams(string? Cursor);
public record Resource(string Name, string Uri, string Kind);
public record ListResourcesResult(string? NextCursor, List<Resource> Resources);

public record ListResourceTemplatesRequestParams(string? Cursor);
public record ResourceTemplate(string Uri, string? Description);
public record ListResourceTemplatesResult(string? NextCursor, List<ResourceTemplate> ResourceTemplates);

public record ListPromptsRequestParams(string? Cursor);
public record Prompt(string Name, string? Description);
public record ListPromptsResult(string? NextCursor, List<Prompt> Prompts);

public record GetPromptRequestParams(string Name, JsonElement? Arguments);
public record PromptMessage(string Role, string Content);
public record GetPromptResult(List<PromptMessage> Messages, string? Description);
public record AddPromptRequestParams(string Name, string Message);

public record ReadResourceRequestParams(string Uri);
public record WriteResourceRequestParams(string Uri, string Text);
public record SubscribeRequestParams(string Uri);
public record UnsubscribeRequestParams(string Uri);

public record SetLevelRequestParams(string Level);
public record Result();

public record CompleteRequestParams(CompleteRequestParamsArgument Argument, CompleteRequestParamsRef Ref);
public record CompleteRequestParamsArgument(string Name, string Value);
public record CompleteRequestParamsRef(string Uri);
public record CompleteResult(CompleteResultCompletion Completion);
public record CompleteResultCompletion(List<string> Values, bool? HasMore, long? Total);

public record ModelHint(string? Name);
public record ModelPreferences(double? CostPriority, List<ModelHint>? Hints, double? IntelligencePriority, double? SpeedPriority);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(SamplingTextContent), typeDiscriminator: "text")]
public abstract record SamplingMessageContent;
public record SamplingTextContent(string Text) : SamplingMessageContent;

public record SamplingMessage(SamplingMessageContent Content, string Role);

public record CreateMessageRequestParams(
    List<SamplingMessage> Messages,
    int MaxTokens,
    string? IncludeContext = null,
    JsonElement? Metadata = null,
    ModelPreferences? ModelPreferences = null,
    List<string>? StopSequences = null,
    string? SystemPrompt = null,
    double? Temperature = null);

public record CreateMessageResult(CreateMessageResultContent Content, string Model, string Role, string? StopReason);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CreateMessageTextContent), typeDiscriminator: "text")]
public abstract record CreateMessageResultContent;
public record CreateMessageTextContent(string Text) : CreateMessageResultContent;

public record GetMessageEntryResult(string? Entry);

public record MessagesResult(List<string> Messages);
public record CountMessagesResult(int Count);
public record SearchMessagesRequestParams(string Term, int Limit);
public record LastMessagesRequestParams(int Count);

