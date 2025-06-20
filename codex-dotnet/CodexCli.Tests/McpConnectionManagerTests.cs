using CodexCli.Util;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;

public class McpConnectionManagerTests
{
    [Fact]
    public void ParseFqNameRoundTrip()
    {
        var fq = McpConnectionManager.FullyQualifiedToolName("srv","tool");
        Assert.True(McpConnectionManager.TryParseFullyQualifiedToolName(fq, out var s, out var t));
        Assert.Equal("srv", s);
        Assert.Equal("tool", t);
    }

    [Fact]
    public void ParseFqNameInvalid()
    {
        Assert.False(McpConnectionManager.TryParseFullyQualifiedToolName("badname", out _, out _));
    }

    [Fact]
    public void ListServersReturnsNames()
    {
        var mgr = (McpConnectionManager)Activator.CreateInstance(typeof(McpConnectionManager), true)!;
        var field = typeof(McpConnectionManager).GetField("_clients", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(mgr, new Dictionary<string, McpClient> { { "a", null! }, { "b", null! } });
        var names = mgr.ListServers().ToList();
        Assert.Contains("a", names);
        Assert.Contains("b", names);
    }

    [Fact]
    public void ListAllToolsAggregates()
    {
        var mgr = (McpConnectionManager)Activator.CreateInstance(typeof(McpConnectionManager), true)!;
        var field = typeof(McpConnectionManager).GetField("_tools", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var tool = new Tool("codex", new ToolInputSchema(null, null, "object"), null, null);
        field.SetValue(mgr, new Dictionary<string, Tool> { { "srv__OAI_CODEX_MCP__codex", tool } });
        var all = mgr.ListAllTools();
        Assert.Single(all);
        Assert.Equal("codex", all["srv__OAI_CODEX_MCP__codex"].Name);
    }

    [Fact]
    public void GetToolNamesReturnsKeys()
    {
        var mgr = (McpConnectionManager)Activator.CreateInstance(typeof(McpConnectionManager), true)!;
        var field = typeof(McpConnectionManager).GetField("_tools", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(mgr, new Dictionary<string, Tool> { { "srv__OAI_CODEX_MCP__codex", new Tool("codex", new ToolInputSchema(null, null, "object"), null, null) } });
        var names = mgr.GetToolNames().ToList();
        Assert.Contains("srv__OAI_CODEX_MCP__codex", names);
    }
}
