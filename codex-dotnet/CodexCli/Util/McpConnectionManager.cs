using System.Text.Json;
using CodexCli.Protocol;

using CodexCli.Config;

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
