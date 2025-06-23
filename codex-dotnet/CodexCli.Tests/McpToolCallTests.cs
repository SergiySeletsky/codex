using CodexCli.Util;
using CodexCli.Models;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using CodexCli.Protocol;

public class McpToolCallTests
{
    [Fact(Skip="flaky in CI")]
    public async Task HandleMcpToolCallSuccess()
    {
        string script = Path.Combine(Path.GetTempPath(), "mcp_tool_success.sh");
        await File.WriteAllTextAsync(script, "read line; echo '{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"content\":[{\"value\":\"ok\"}],\"isError\":false}}'");
        try
        {
            var servers = new Dictionary<string, McpServerConfig> { { "test", new McpServerConfig("bash", new List<string>{ script }, null) } };
            var (mgr, _) = await McpConnectionManager.CreateAsync(servers);
            var ch = Channel.CreateUnbounded<Event>();
            var res = await McpToolCall.HandleMcpToolCallAsync(mgr, ch.Writer, "s1", "1", "test", "codex", "{}");
            var begin = await ch.Reader.ReadAsync();
            Assert.IsType<McpToolCallBeginEvent>(begin);
            var end = await ch.Reader.ReadAsync();
            var endEvt = Assert.IsType<McpToolCallEndEvent>(end);
            Assert.True(endEvt.IsSuccess);
            Assert.Contains("ok", endEvt.ResultJson);
            var mcp = Assert.IsType<McpToolCallOutputInputItem>(res);
            Assert.Contains("ok", mcp.ResultJson);
        }
        finally
        {
            File.Delete(script);
        }
    }

    [Fact(Skip="flaky in CI")]
    public async Task HandleMcpToolCallError()
    {
        string script = Path.Combine(Path.GetTempPath(), "mcp_tool_err.sh");
        await File.WriteAllTextAsync(script, "read line; echo 'notjson'");
        try
        {
            var servers = new Dictionary<string, McpServerConfig> { { "test", new McpServerConfig("bash", new List<string>{ script }, null) } };
            var (mgr, _) = await McpConnectionManager.CreateAsync(servers);
            var ch = Channel.CreateUnbounded<Event>();
            var res = await McpToolCall.HandleMcpToolCallAsync(mgr, ch.Writer, "s2", "2", "test", "codex", "{}");
            var begin = await ch.Reader.ReadAsync();
            Assert.IsType<McpToolCallBeginEvent>(begin);
            var end = await ch.Reader.ReadAsync();
            var endEvt = Assert.IsType<McpToolCallEndEvent>(end);
            Assert.False(endEvt.IsSuccess);
            Assert.Contains("error", endEvt.ResultJson);
            var mcp = Assert.IsType<McpToolCallOutputInputItem>(res);
            Assert.Contains("error", mcp.ResultJson);
        }
        finally
        {
            File.Delete(script);
        }
    }
}
