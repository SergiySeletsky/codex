using System.CommandLine;
using CodexCli.Config;
using System.Net;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace CodexCli.Commands;

public static class McpCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var portOpt = new Option<int>("--port", () => 8080, "Port to listen on");
        var cmd = new Command("mcp", "Run as MCP server");
        cmd.AddOption(portOpt);
        cmd.SetHandler(async (string? cfgPath, string? cd, int port) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);
            using var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
            Console.WriteLine($"MCP server listening on port {port}");

            var connections = new List<StreamWriter>();

            _ = Task.Run(async () =>
            {
                await foreach (var ev in CodexCli.Protocol.MockCodexAgent.RunAsync("hello", Array.Empty<string>()))
                {
                    var line = $"data: {ev.GetType().Name}\n\n";
                    lock (connections)
                    {
                        foreach (var w in connections.ToList())
                        {
                            try
                            {
                                w.Write(line);
                                w.Flush();
                            }
                            catch
                            {
                                connections.Remove(w);
                            }
                        }
                    }
                }
            });

            while (true)
            {
                var ctx = await listener.GetContextAsync();
                if (ctx.Request.Url?.AbsolutePath == "/events")
                {
                    ctx.Response.ContentType = "text/event-stream";
                    var writer = new StreamWriter(ctx.Response.OutputStream);
                    lock (connections) connections.Add(writer);
                }
                else
                {
                    using var reader = new StreamReader(ctx.Request.InputStream);
                    var body = await reader.ReadToEndAsync();
                    Console.WriteLine($"Received: {body}");
                    var resp = Encoding.UTF8.GetBytes("ok");
                    ctx.Response.ContentLength64 = resp.Length;
                    await ctx.Response.OutputStream.WriteAsync(resp);
                    ctx.Response.Close();
                }
            }
        }, configOption, cdOption, portOpt);
        return cmd;
    }
}
