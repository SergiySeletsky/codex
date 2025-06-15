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
        foreach (var kv in servers)
        {
            try
            {
                var client = await McpClient.StartAsync(kv.Value.Command, kv.Value.Args, kv.Value.Env);
                mgr._clients[kv.Key] = client;
                var tools = await client.ListToolsAsync();
                foreach (var t in tools.Tools)
                {
                    var fq = FullyQualifiedToolName(kv.Key, t.Name);
                    mgr._tools[fq] = t;
                }
            }
            catch (Exception ex)
            {
                errors[kv.Key] = ex;
            }
        }
        return (mgr, errors);
    }

    public Dictionary<string, Tool> ListAllTools() => new(_tools);

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
