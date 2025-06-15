using System.Net;
using System.Text.Json;
using System.IO;
using System.Linq;
using CodexCli.Protocol;

namespace CodexCli.Util;

public class McpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly List<StreamWriter> _eventClients = new();
    private readonly Dictionary<string, string> _resources = new();
    private readonly string _root;

    public McpServer(int port)
    {
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _root = Directory.GetCurrentDirectory();
        // simple in-memory resource store for demo purposes
        _resources["mem:/demo.txt"] = "Hello from MCP";
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
            "initialize" => Task.FromResult(CreateResponse(id, new { server = "codex-mcp" })),
            "ping" => Task.FromResult(CreateResponse(id, new { })),
            "tools/list" => Task.FromResult(CreateResponse(id, new { tools = new[] { CreateCodexTool() } })),
            "tools/call" => HandleCallToolAsync(req),
            "roots/list" => Task.FromResult(CreateResponse(id, new { roots = new[] { new { uri = _root } } })),
            "resources/list" => Task.FromResult(CreateResponse(id, new { resources = _resources.Keys.Select(u => new { name = Path.GetFileName(u), uri = u, kind = "file" }), nextCursor = (string?)null })),
            "resources/templates/list" => Task.FromResult(CreateResponse(id, new { resourceTemplates = Array.Empty<object>(), nextCursor = (string?)null })),
            "resources/read" => HandleReadResourceAsync(req),
            "resources/subscribe" => Task.FromResult(CreateResponse(id, new { })),
            "resources/unsubscribe" => Task.FromResult(CreateResponse(id, new { })),
            "prompts/list" => Task.FromResult(CreateResponse(id, new { prompts = Array.Empty<object>(), nextCursor = (string?)null })),
            "prompts/get" => Task.FromResult(CreateResponse(id, new { messages = Array.Empty<object>(), description = (string?)null })),
            "logging/setLevel" => Task.FromResult(CreateResponse(id, new { })),
            "completion/complete" => Task.FromResult(CreateResponse(id, new { completion = new { values = Array.Empty<string>(), hasMore = (bool?)null, total = (int?)null } })),
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
