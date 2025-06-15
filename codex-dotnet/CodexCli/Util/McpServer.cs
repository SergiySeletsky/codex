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
            return Task.FromResult(new JsonRpcMessage{ Id = req?.Id });
        }

        return req.Method switch
        {
            "initialize" => Task.FromResult(new JsonRpcMessage
            {
                Id = req.Id,
                Result = JsonSerializer.SerializeToElement(new { server = "codex-mcp" })
            }),
            "ping" => Task.FromResult(new JsonRpcMessage { Id = req.Id, Result = JsonSerializer.SerializeToElement(new { }) }),
            "tools/list" => Task.FromResult(new JsonRpcMessage
            {
                Id = req.Id,
                Result = JsonSerializer.SerializeToElement(new { tools = new[] { new { name = "codex" } } })
            }),
            "tools/call" => Task.FromResult(new JsonRpcMessage
            {
                Id = req.Id,
                Result = JsonSerializer.SerializeToElement(new { content = new[] { JsonSerializer.SerializeToElement("ok") } })
            }),
            "resources/list" => Task.FromResult(new JsonRpcMessage
            {
                Id = req.Id,
                Result = JsonSerializer.SerializeToElement(new { resources = Array.Empty<object>(), nextCursor = (string?)null })
            }),
            "resources/templates/list" => Task.FromResult(new JsonRpcMessage
            {
                Id = req.Id,
                Result = JsonSerializer.SerializeToElement(new { resourceTemplates = Array.Empty<object>(), nextCursor = (string?)null })
            }),
            "resources/read" => Task.FromResult(new JsonRpcMessage { Id = req.Id, Result = JsonSerializer.SerializeToElement(new { }) }),
            "resources/subscribe" => Task.FromResult(new JsonRpcMessage { Id = req.Id, Result = JsonSerializer.SerializeToElement(new { }) }),
            "resources/unsubscribe" => Task.FromResult(new JsonRpcMessage { Id = req.Id, Result = JsonSerializer.SerializeToElement(new { }) }),
            "prompts/list" => Task.FromResult(new JsonRpcMessage
            {
                Id = req.Id,
                Result = JsonSerializer.SerializeToElement(new { prompts = Array.Empty<object>(), nextCursor = (string?)null })
            }),
            "prompts/get" => Task.FromResult(new JsonRpcMessage
            {
                Id = req.Id,
                Result = JsonSerializer.SerializeToElement(new { messages = Array.Empty<object>(), description = (string?)null })
            }),
            "logging/setLevel" => Task.FromResult(new JsonRpcMessage { Id = req.Id, Result = JsonSerializer.SerializeToElement(new { }) }),
            "completion/complete" => Task.FromResult(new JsonRpcMessage
            {
                Id = req.Id,
                Result = JsonSerializer.SerializeToElement(new { completion = new { values = Array.Empty<string>(), hasMore = (bool?)null, total = (int?)null } })
            }),
            _ => Task.FromResult(new JsonRpcMessage { Id = req.Id, Result = JsonSerializer.SerializeToElement(new { }) })
        };
    }

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
