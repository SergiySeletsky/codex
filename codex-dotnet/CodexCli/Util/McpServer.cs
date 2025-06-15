using System.Net;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CodexCli.Protocol;

namespace CodexCli.Util;

public class McpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly List<StreamWriter> _eventClients = new();
    private readonly Dictionary<string, string> _resources = new();
    private readonly Dictionary<string, List<PromptMessage>> _prompts = new();
    private readonly List<ResourceTemplate> _templates = new();
    private readonly string _root;
    private readonly string _storagePath;
    private readonly HashSet<string> _subscriptions = new();
    private string _logLevel = "info";

    public McpServer(int port)
    {
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _root = Directory.GetCurrentDirectory();
        _storagePath = Path.Combine(_root, "mcp-resources.json");

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

        _prompts["demo"] = new List<PromptMessage> { new("system", "Say hello") };
        _templates.Add(new ResourceTemplate("mem:/template.txt", "Demo template"));
        SaveResources();
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
            "roots/list" => Task.FromResult(CreateResponse(id, new { roots = new[] { new { uri = _root } } })),
            "resources/list" => Task.FromResult(CreateResponse(id, new { resources = _resources.Keys.Select(u => new { name = Path.GetFileName(u), uri = u, kind = "file" }), nextCursor = (string?)null })),
            "resources/templates/list" => Task.FromResult(CreateResponse(id, new { resourceTemplates = _templates.Select(t => new { uri = t.Uri, description = t.Description }), nextCursor = (string?)null })),
            "resources/read" => HandleReadResourceAsync(req),
            "resources/write" => HandleWriteResourceAsync(req),
            "resources/subscribe" => HandleSubscribeAsync(req),
            "resources/unsubscribe" => HandleUnsubscribeAsync(req),
            "prompts/list" => Task.FromResult(CreateResponse(id, new { prompts = _prompts.Keys.Select(n => new { name = n, description = (string?)null }), nextCursor = (string?)null })),
            "prompts/get" => HandleGetPromptAsync(req),
            "logging/setLevel" => HandleSetLevelAsync(req),
            "completion/complete" => HandleCompleteAsync(req),
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
        _resources[uri] = text;
        var path = UriToPath(uri);
        if (path != null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, text);
        }
        SaveResources();
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

    private Task<JsonRpcMessage> HandleSetLevelAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        if (req.Params != null && req.Params.Value.TryGetProperty("level", out var l))
            _logLevel = l.GetString() ?? _logLevel;
        return Task.FromResult(CreateResponse(id, new { }));
    }

    private Task<JsonRpcMessage> HandleCompleteAsync(JsonRpcMessage req)
    {
        var id = req.Id ?? JsonDocument.Parse("0").RootElement;
        var result = new { completion = new { values = new[] { "demo completion" }, hasMore = (bool?)null, total = (int?)null } };
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
}
