using System.Net;
using System.Text.Json;
using CodexCli.Protocol;

namespace CodexCli.Util;

public class McpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly List<StreamWriter> _eventClients = new();

    public McpServer(int port)
    {
        _listener.Prefixes.Add($"http://localhost:{port}/");
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
            "tools/list" => Task.FromResult(CreateResponse(id, new { tools = new[] { new { name = "codex" } } })),
            "tools/call" => HandleCallToolAsync(req),
            "resources/list" => Task.FromResult(CreateResponse(id, new { resources = Array.Empty<object>(), nextCursor = (string?)null })),
            "resources/templates/list" => Task.FromResult(CreateResponse(id, new { resourceTemplates = Array.Empty<object>(), nextCursor = (string?)null })),
            "resources/read" => Task.FromResult(CreateResponse(id, new { })),
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

    private static JsonRpcMessage CreateResponse(JsonElement id, object result) =>
        new()
        {
            Id = id,
            Result = JsonSerializer.SerializeToElement(result)
        };

    public void EmitEvent(Event ev)
    {
        var line = $"data: {ev.GetType().Name}\n\n";
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
