using System.Net;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CodexCli.Protocol;

namespace CodexCli.Util;

/// <summary>
/// Mirrors codex-rs/mcp-server/src/message_processor.rs (ping, event stream,
/// watch-events, messages add/clear, resource write/update subscribe and
/// prompt/root add/remove event parity tested).
/// </summary>
public class McpServer : IDisposable, IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly List<StreamWriter> _eventClients = new();
    private readonly Dictionary<string, string> _resources = new();
    private readonly Dictionary<string, List<PromptMessage>> _prompts = new();
    private readonly List<ResourceTemplate> _templates = new();
    private readonly List<string> _roots = new();
    private readonly string _root;
    private readonly string _storagePath;
    private readonly string _promptsPath;
    private readonly string _rootsPath;
    private readonly string _messagesPath;
    private readonly List<string> _messages = new();
    private readonly HashSet<string> _subscriptions = new();
    private string _logLevel = "info";

    public McpServer(int port)
    {
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _root = Directory.GetCurrentDirectory();
        _storagePath = Path.Combine(_root, "mcp-resources.json");
        _promptsPath = Path.Combine(_root, "mcp-prompts.json");
        _rootsPath = Path.Combine(_root, "mcp-roots.json");
        _messagesPath = Path.Combine(_root, "mcp-messages.json");

        // simple in-memory resource store for demo purposes
        _resources["mem:/demo.txt"] = "Hello from MCP";
        if (File.Exists(_storagePath))
        {
            try
            {
                var json = File.ReadAllText(_storagePath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (loaded != null)
                {
                    foreach (var kv in loaded)
                        _resources[kv.Key] = kv.Value;
                }
            }
            catch { }
        }

        if (File.Exists(_promptsPath))
        {
            try
            {
                var json = File.ReadAllText(_promptsPath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, List<PromptMessage>>>(json);
                if (loaded != null)
                {
                    foreach (var kv in loaded)
                        _prompts[kv.Key] = kv.Value;
                }
            }
            catch { }
        }

        if (File.Exists(_messagesPath))
        {
            try
            {
                var json = File.ReadAllText(_messagesPath);
                var loaded = JsonSerializer.Deserialize<List<string>>(json);
                if (loaded != null) _messages.AddRange(loaded);
            }
            catch { }
        }

        if (File.Exists(_rootsPath))
        {
            try
            {
                var json = File.ReadAllText(_rootsPath);
                var loaded = JsonSerializer.Deserialize<List<string>>(json);
                if (loaded != null) _roots.AddRange(loaded);
            }
            catch { }
        }
        if (_roots.Count == 0) _roots.Add(_root);

        _prompts.TryAdd("demo", new List<PromptMessage> { new("system", "Say hello") });
        _templates.Add(new ResourceTemplate("mem:/template.txt", "Demo template"));
        SaveResources();
        SavePrompts();
        SaveMessages();
        SaveRoots();
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _listener.Start();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = HandleContextAsync(ctx, cancellationToken);
            }
        }
        catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task HandleContextAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "/";
        if (path == "/events")
        {
            ctx.Response.ContentType = "text/event-stream";
            var writer = new StreamWriter(ctx.Response.OutputStream);
            lock (_eventClients) _eventClients.Add(writer);
        }
        else if (path == "/jsonrpc")
        {
            using var reader = new StreamReader(ctx.Request.InputStream);
            var body = await reader.ReadToEndAsync().ConfigureAwait(false);
            var req = JsonSerializer.Deserialize<JsonRpcMessage>(body);
            var resp = await HandleRequestAsync(req);
            var json = JsonSerializer.Serialize(resp);
            ctx.Response.ContentType = "application/json";
            await ctx.Response.OutputStream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(json), ct);
            ctx.Response.Close();
        }
        else
        {
            ctx.Response.StatusCode = 404;
            ctx.Response.Close();
        }
    }

    private Task<JsonRpcMessage> HandleRequestAsync(JsonRpcMessage? req)
    {
        if (req == null || req.Method == null)
        {
            return Task.FromResult(new JsonRpcMessage { Id = req?.Id });
        }

        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        return req.Method switch
        {
            "initialize" => Task.FromResult(CreateResponse(id, new {
                serverInfo = new { name = "codex-mcp", version = "1.0" },
                protocolVersion = "2025-03-26",
                capabilities = new {
                    resources = new { subscribe = true, listChanged = (bool?)null },
                    tools = new { listChanged = (bool?)null },
                    prompts = new { listChanged = (bool?)null },
                    logging = (object?)null,
                    completions = (object?)null,
                    experimental = (object?)null
                }
            })),
            "ping" => Task.FromResult(CreateResponse(id, new { })),
            "tools/list" => Task.FromResult(CreateResponse(id, new { tools = new[] { CreateCodexTool() } })),
            "tools/call" => HandleCallToolAsync(req),
            "roots/list" => Task.FromResult(CreateResponse(id, new { roots = _roots.Select(r => new { uri = r }) })),
            "roots/add" => HandleAddRootAsync(req),
            "roots/remove" => HandleRemoveRootAsync(req),
            "resources/list" => Task.FromResult(CreateResponse(id, new { resources = _resources.Keys.Select(u => new { name = Path.GetFileName(u), uri = u, kind = "file" }), nextCursor = (string?)null })),
            "resources/templates/list" => Task.FromResult(CreateResponse(id, new { resourceTemplates = _templates.Select(t => new { uri = t.Uri, description = t.Description }), nextCursor = (string?)null })),
            "resources/read" => HandleReadResourceAsync(req),
            "resources/write" => HandleWriteResourceAsync(req),
            "resources/subscribe" => HandleSubscribeAsync(req),
            "resources/unsubscribe" => HandleUnsubscribeAsync(req),
            "prompts/list" => Task.FromResult(CreateResponse(id, new { prompts = _prompts.Keys.Select(n => new { name = n, description = (string?)null }), nextCursor = (string?)null })),
            "prompts/get" => HandleGetPromptAsync(req),
            "prompts/add" => HandleAddPromptAsync(req),
            "logging/setLevel" => HandleSetLevelAsync(req),
            "completion/complete" => HandleCompleteAsync(req),
            "sampling/createMessage" => HandleCreateMessageAsync(req),
            "messages/add" => HandleAddMessageAsync(req),
            "messages/getEntry" => HandleGetMessageAsync(req),
            "messages/list" => HandleListMessagesAsync(req),
            "messages/count" => HandleCountMessagesAsync(req),
            "messages/clear" => HandleClearMessagesAsync(req),
            "messages/search" => HandleSearchMessagesAsync(req),
            "messages/last" => HandleLastMessagesAsync(req),
            _ => Task.FromResult(CreateResponse(id, new { }))
        };
    }

    private async Task<JsonRpcMessage> HandleCallToolAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        if (req.Params == null)
            return CreateResponse(id, new { });

        var callObj = req.Params.Value;
        string? name = null;
        JsonElement? args = null;
        if (callObj.TryGetProperty("name", out var n)) name = n.GetString();
        if (callObj.TryGetProperty("arguments", out var a)) args = a;

        if (name != "codex")
        {
            return CreateResponse(id, new
            {
                content = new[] { JsonSerializer.SerializeToElement($"Unknown tool '{name}'") },
                isError = true
            });
        }

        CodexToolCallParam? param = null;
        if (args.HasValue)
        {
            try
            {
                param = args.Value.Deserialize<CodexToolCallParam>();
            }
            catch
            {
                // ignore
            }
        }

        param ??= new CodexToolCallParam(string.Empty);
        var result = await CodexToolRunner.RunCodexToolSessionAsync(param, EmitEvent);
        return CreateResponse(id, result);
    }

    private Task<JsonRpcMessage> HandleReadResourceAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        if (req.Params == null) return Task.FromResult(CreateResponse(id, new { contents = Array.Empty<object>() }));
        string? uri = null;
        if (req.Params.Value.TryGetProperty("uri", out var u)) uri = u.GetString();
        if (uri == null || !_resources.TryGetValue(uri, out var text))
            return Task.FromResult(CreateResponse(id, new { contents = Array.Empty<object>() }));
        var result = new { contents = new[] { new { text } } };
        return Task.FromResult(CreateResponse(id, result));
    }

    private async Task<JsonRpcMessage> HandleWriteResourceAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        if (req.Params == null) return CreateResponse(id, new { });
        var obj = req.Params.Value;
        if (!obj.TryGetProperty("uri", out var u) || !obj.TryGetProperty("text", out var t))
            return CreateResponse(id, new { });
        var uri = u.GetString();
        var text = t.GetString() ?? string.Empty;
        if (uri == null) return CreateResponse(id, new { });
        bool added = !_resources.ContainsKey(uri);
        EmitEvent(new ProgressNotificationEvent(Guid.NewGuid().ToString(), "write", 0.0, JsonDocument.Parse("0").RootElement, 1.0));
        _resources[uri] = text;
        var path = UriToPath(uri);
        if (path != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, text);
        }
        SaveResources();
        EmitEvent(new ProgressNotificationEvent(Guid.NewGuid().ToString(), "write", 1.0, JsonDocument.Parse("0").RootElement, 1.0));
        if (added) EmitEvent(new ResourceListChangedEvent(Guid.NewGuid().ToString()));
        if (_subscriptions.Contains(uri))
            EmitEvent(new ResourceUpdatedEvent(Guid.NewGuid().ToString(), uri));
        return CreateResponse(id, new { });
    }

    private Task<JsonRpcMessage> HandleSubscribeAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        if (req.Params != null && req.Params.Value.TryGetProperty("uri", out var u))
        {
            var uri = u.GetString();
            if (uri != null) _subscriptions.Add(uri);
        }
        return Task.FromResult(CreateResponse(id, new { }));
    }

    private Task<JsonRpcMessage> HandleUnsubscribeAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        if (req.Params != null && req.Params.Value.TryGetProperty("uri", out var u))
        {
            var uri = u.GetString();
            if (uri != null) _subscriptions.Remove(uri);
        }
        return Task.FromResult(CreateResponse(id, new { }));
    }

    private Task<JsonRpcMessage> HandleGetPromptAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        if (req.Params == null) return Task.FromResult(CreateResponse(id, new { messages = Array.Empty<object>(), description = (string?)null }));
        if (!req.Params.Value.TryGetProperty("name", out var n))
            return Task.FromResult(CreateResponse(id, new { messages = Array.Empty<object>(), description = (string?)null }));
        var name = n.GetString();
        if (name == null || !_prompts.TryGetValue(name, out var msgs))
            return Task.FromResult(CreateResponse(id, new { messages = Array.Empty<object>(), description = (string?)null }));
        var result = new { messages = msgs.Select(m => new { role = m.Role, content = m.Content }), description = (string?)null };
        return Task.FromResult(CreateResponse(id, result));
    }

    private Task<JsonRpcMessage> HandleAddPromptAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        if (req.Params == null) return Task.FromResult(CreateResponse(id, new { }));
        var obj = req.Params.Value;
        if (!obj.TryGetProperty("name", out var n) || !obj.TryGetProperty("message", out var m))
            return Task.FromResult(CreateResponse(id, new { }));
        var name = n.GetString();
        var message = m.GetString();
        if (string.IsNullOrEmpty(name) || message == null)
            return Task.FromResult(CreateResponse(id, new { }));
        _prompts[name] = new List<PromptMessage> { new("system", message) };
        SavePrompts();
        EmitEvent(new PromptListChangedEvent(Guid.NewGuid().ToString()));
        return Task.FromResult(CreateResponse(id, new { }));
    }

    private Task<JsonRpcMessage> HandleAddRootAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        if (req.Params == null) return Task.FromResult(CreateResponse(id, new { }));
        if (!req.Params.Value.TryGetProperty("uri", out var u))
            return Task.FromResult(CreateResponse(id, new { }));
        var uri = u.GetString();
        if (string.IsNullOrEmpty(uri))
            return Task.FromResult(CreateResponse(id, new { }));
        if (!_roots.Contains(uri))
        {
            _roots.Add(uri);
            SaveRoots();
            EmitEvent(new RootsListChangedEvent(Guid.NewGuid().ToString()));
        }
        return Task.FromResult(CreateResponse(id, new { }));
    }

    private Task<JsonRpcMessage> HandleRemoveRootAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        if (req.Params == null) return Task.FromResult(CreateResponse(id, new { }));
        if (!req.Params.Value.TryGetProperty("uri", out var u))
            return Task.FromResult(CreateResponse(id, new { }));
        var uri = u.GetString();
        if (string.IsNullOrEmpty(uri))
            return Task.FromResult(CreateResponse(id, new { }));
        if (_roots.Remove(uri))
        {
            SaveRoots();
            EmitEvent(new RootsListChangedEvent(Guid.NewGuid().ToString()));
        }
        return Task.FromResult(CreateResponse(id, new { }));
    }

    private Task<JsonRpcMessage> HandleSetLevelAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        if (req.Params != null && req.Params.Value.TryGetProperty("level", out var l))
        {
            _logLevel = l.GetString() ?? _logLevel;
            EmitEvent(new LoggingMessageEvent(Guid.NewGuid().ToString(), $"log level set to {_logLevel}"));
        }
        return Task.FromResult(CreateResponse(id, new { }));
    }

    private Task<JsonRpcMessage> HandleCompleteAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        var result = new { completion = new { values = new[] { "demo completion" }, hasMore = (bool?)null, total = (int?)null } };
        return Task.FromResult(CreateResponse(id, result));
    }

    private Task<JsonRpcMessage> HandleCreateMessageAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        if (req.Params == null)
            return Task.FromResult(CreateResponse(id, new { content = new { text = string.Empty }, model = "demo", role = "assistant" }));
        string text = "";
        if (req.Params.Value.TryGetProperty("messages", out var msgs) && msgs.ValueKind == JsonValueKind.Array && msgs.GetArrayLength() > 0)
        {
            var msg0 = msgs[0];
            if (msg0.TryGetProperty("content", out var c) && c.TryGetProperty("text", out var t))
                text = t.GetString() ?? "";
        }
        var result = new { content = new { text = $"echo {text}" }, model = "demo", role = "assistant", stopReason = (string?)null };
        return Task.FromResult(CreateResponse(id, result));
    }

    private Task<JsonRpcMessage> HandleAddMessageAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        if (req.Params != null && req.Params.Value.TryGetProperty("text", out var t))
        {
            var text = t.GetString() ?? string.Empty;
            _messages.Add(text);
            SaveMessages();
            EmitEvent(new AddToHistoryEvent(Guid.NewGuid().ToString(), text));
        }
        return Task.FromResult(CreateResponse(id, new { }));
    }

    private Task<JsonRpcMessage> HandleGetMessageAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        int offset = 0;
        if (req.Params != null && req.Params.Value.TryGetProperty("offset", out var o))
            offset = o.GetInt32();
        string? entry = offset >=0 && offset < _messages.Count ? _messages[offset] : null;
        EmitEvent(new GetHistoryEntryResponseEvent(Guid.NewGuid().ToString(), "default", offset, entry));
        var result = new { entry };
        return Task.FromResult(CreateResponse(id, result));
    }

    private Task<JsonRpcMessage> HandleListMessagesAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        var result = new { messages = _messages.ToList(), nextCursor = (string?)null };
        return Task.FromResult(CreateResponse(id, result));
    }

    private Task<JsonRpcMessage> HandleCountMessagesAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        var result = new { count = _messages.Count };
        return Task.FromResult(CreateResponse(id, result));
    }

    private Task<JsonRpcMessage> HandleClearMessagesAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        _messages.Clear();
        SaveMessages();
        EmitEvent(new LoggingMessageEvent(Guid.NewGuid().ToString(), "messages cleared"));
        return Task.FromResult(CreateResponse(id, new { }));
    }

    private Task<JsonRpcMessage> HandleSearchMessagesAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        string term = string.Empty;
        int limit = 10;
        if (req.Params != null)
        {
            if (req.Params.Value.TryGetProperty("term", out var t)) term = t.GetString() ?? string.Empty;
            if (req.Params.Value.TryGetProperty("limit", out var l)) limit = l.GetInt32();
        }
        var resultLines = _messages.Where(m => m.Contains(term, StringComparison.OrdinalIgnoreCase)).Take(limit).ToList();
        var result = new { messages = resultLines };
        return Task.FromResult(CreateResponse(id, result));
    }

    private Task<JsonRpcMessage> HandleLastMessagesAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        int count = 10;
        if (req.Params != null && req.Params.Value.TryGetProperty("count", out var c))
            count = c.GetInt32();
        var last = _messages.TakeLast(count).ToList();
        var result = new { messages = last };
        return Task.FromResult(CreateResponse(id, result));
    }

    private static string? UriToPath(string uri)
        => uri.StartsWith("file:/") ? uri.Substring(6) : null;

    private void SaveResources()
    {
        try
        {
            var json = JsonSerializer.Serialize(_resources);
            File.WriteAllText(_storagePath, json);
        }
        catch { }
    }

    private void SavePrompts()
    {
        try
        {
            var json = JsonSerializer.Serialize(_prompts);
            File.WriteAllText(_promptsPath, json);
        }
        catch { }
    }

    private void SaveMessages()
    {
        try
        {
            var json = JsonSerializer.Serialize(_messages);
            File.WriteAllText(_messagesPath, json);
        }
        catch { }
    }

    private void SaveRoots()
    {
        try
        {
            var json = JsonSerializer.Serialize(_roots);
            File.WriteAllText(_rootsPath, json);
        }
        catch { }
    }

    private static object CreateCodexTool() => new
    {
        name = "codex",
        inputSchema = new
        {
            type = "object",
            properties = new { prompt = new { type = "string" } },
            required = new[] { "prompt" }
        },
        description = "Run a Codex session."
    };

    private static JsonRpcMessage CreateResponse(JsonElement id, object result) =>
        new()
        {
            Id = id,
            Result = JsonSerializer.SerializeToElement(result)
        };

    public void EmitEvent(Event ev)
    {
        var json = JsonSerializer.Serialize(ev);
        var line = $"data: {json}\n\n";
        lock (_eventClients)
        {
            foreach (var w in _eventClients.ToList())
            {
                try
                {
                    w.Write(line);
                    w.Flush();
                }
                catch
                {
                    _eventClients.Remove(w);
                }
            }
        }
    }

    public void Dispose()
    {
        _listener.Close();
        lock (_eventClients)
        {
            foreach (var w in _eventClients) w.Dispose();
            _eventClients.Clear();
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
