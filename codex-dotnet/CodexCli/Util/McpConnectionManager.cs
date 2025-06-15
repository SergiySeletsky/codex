using System.Text.Json;
using CodexCli.Protocol;

using CodexCli.Config;

// Port of codex-rs/core/src/mcp_connection_manager.rs (done)

namespace CodexCli.Util;

public record McpServerConfig(string Command, List<string> Args, Dictionary<string,string>? Env);

public class McpConnectionManager
{
    private readonly Dictionary<string, McpClient> _clients = new();
    private readonly Dictionary<string, Tool> _tools = new();

    private McpConnectionManager() {}

    public static async Task<(McpConnectionManager, Dictionary<string,Exception>)> CreateAsync(Dictionary<string, McpServerConfig> servers)
    {
        var mgr = new McpConnectionManager();
        var errors = new Dictionary<string,Exception>();
        var tasks = servers.Select(async kv =>
        {
            try
            {
                var client = await McpClient.StartAsync(kv.Value.Command, kv.Value.Args, kv.Value.Env);
                lock (mgr)
                {
                    mgr._clients[kv.Key] = client;
                }
                var tools = await client.ListToolsAsync();
                lock (mgr)
                {
                    foreach (var t in tools.Tools)
                    {
                        var fq = FullyQualifiedToolName(kv.Key, t.Name);
                        mgr._tools[fq] = t;
                    }
                }
            }
            catch (Exception ex)
            {
                lock (errors) { errors[kv.Key] = ex; }
            }
        }).ToList();
        await Task.WhenAll(tasks);
        return (mgr, errors);
    }

    public async Task RefreshToolsAsync()
    {
        var local = _clients.ToArray();
        var join = local.Select(async kv => (kv.Key, await kv.Value.ListToolsAsync()));
        var results = await Task.WhenAll(join);
        lock (this)
        {
            _tools.Clear();
            foreach (var (server, res) in results)
                foreach (var t in res.Tools)
                    _tools[FullyQualifiedToolName(server, t.Name)] = t;
        }
    }

    public static Task<(McpConnectionManager, Dictionary<string,Exception>)> CreateAsync(AppConfig cfg)
        => CreateAsync(cfg.McpServers);

    public Dictionary<string, Tool> ListAllTools() => new(_tools);

    public IEnumerable<string> GetToolNames() => _tools.Keys.ToList();

    public IEnumerable<string> ListServers() => _clients.Keys.ToList();

    public async Task<List<Tool>> ListToolsAsync(string server)
    {
        if (!_clients.TryGetValue(server, out var client))
            throw new InvalidOperationException($"unknown MCP server '{server}'");
        var res = await client.ListToolsAsync();
        return res.Tools;
    }

    public bool HasServer(string name) => _clients.ContainsKey(name);

    public async Task<CallToolResult> CallToolAsync(string fqName, JsonElement? args = null, TimeSpan? timeout = null)
    {
        if(!TryParseFullyQualifiedToolName(fqName, out var server, out var tool))
            throw new ArgumentException("invalid tool name", nameof(fqName));
        if(!_clients.TryGetValue(server, out var client))
            throw new InvalidOperationException($"unknown MCP server '{server}'");
        int seconds = (int)(timeout?.TotalSeconds ?? 10);
        return await client.CallToolAsync(tool, args, seconds);
    }

    public async Task<ListRootsResult> ListRootsAsync(string server)
    {
        if(!_clients.TryGetValue(server, out var client))
            throw new InvalidOperationException($"unknown MCP server '{server}'");
        return await client.ListRootsAsync();
    }

    public async Task AddRootAsync(string server, string uri)
    {
        if(!_clients.TryGetValue(server, out var client))
            throw new InvalidOperationException($"unknown MCP server '{server}'");
        await client.AddRootAsync(uri);
    }

    public async Task RemoveRootAsync(string server, string uri)
    {
        if(!_clients.TryGetValue(server, out var client))
            throw new InvalidOperationException($"unknown MCP server '{server}'");
        await client.RemoveRootAsync(uri);
    }

    // Methods below map to codex-rs/core/src/mcp_connection_manager.rs message helpers
    // (C# version done)

    public async Task<MessagesResult> ListMessagesAsync(string server)
    {
        if(!_clients.TryGetValue(server, out var client))
            throw new InvalidOperationException($"unknown MCP server '{server}'");
        return await client.ListMessagesAsync();
    }

    public async Task<CountMessagesResult> CountMessagesAsync(string server)
    {
        if(!_clients.TryGetValue(server, out var client))
            throw new InvalidOperationException($"unknown MCP server '{server}'");
        return await client.CountMessagesAsync();
    }

    public async Task ClearMessagesAsync(string server)
    {
        if(!_clients.TryGetValue(server, out var client))
            throw new InvalidOperationException($"unknown MCP server '{server}'");
        await client.ClearMessagesAsync();
    }

    public async Task<MessagesResult> SearchMessagesAsync(string server, string term)
    {
        if(!_clients.TryGetValue(server, out var client))
            throw new InvalidOperationException($"unknown MCP server '{server}'");
        return await client.SearchMessagesAsync(term);
    }

    public async Task<MessagesResult> LastMessagesAsync(string server, int count)
    {
        if(!_clients.TryGetValue(server, out var client))
            throw new InvalidOperationException($"unknown MCP server '{server}'");
        return await client.LastMessagesAsync(count);
    }

    // Prompt helpers map to codex-rs MCP manager (C# version done)

    public async Task<ListPromptsResult> ListPromptsAsync(string server)
    {
        if(!_clients.TryGetValue(server, out var client))
            throw new InvalidOperationException($"unknown MCP server '{server}'");
        return await client.ListPromptsAsync();
    }

    public async Task<GetPromptResult> GetPromptAsync(string server, string name)
    {
        if(!_clients.TryGetValue(server, out var client))
            throw new InvalidOperationException($"unknown MCP server '{server}'");
        return await client.GetPromptAsync(name);
    }

    public async Task AddPromptAsync(string server, string name, string message)
    {
        if(!_clients.TryGetValue(server, out var client))
            throw new InvalidOperationException($"unknown MCP server '{server}'");
        await client.AddPromptAsync(new AddPromptRequestParams(name, message));
    }

    public static string FullyQualifiedToolName(string server, string tool) => $"{server}{Delimiter}{tool}";
    public static bool TryParseFullyQualifiedToolName(string fq, out string server, out string tool)
    {
        var parts = fq.Split(Delimiter);
        if (parts.Length == 2 && !string.IsNullOrEmpty(parts[0]) && !string.IsNullOrEmpty(parts[1]))
        {
            server = parts[0];
            tool = parts[1];
            return true;
        }
        server = tool = string.Empty;
        return false;
    }

    private const string Delimiter = "__OAI_CODEX_MCP__";
}
