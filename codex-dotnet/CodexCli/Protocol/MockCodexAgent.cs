using CodexCli.Protocol;

namespace CodexCli.Protocol;

public static class MockCodexAgent
{
    public static async IAsyncEnumerable<Event> RunAsync(string prompt)
    {
        await Task.Delay(50);
        yield return new SessionConfiguredEvent(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "gpt-4");
        await Task.Delay(50);
        yield return new AgentMessageEvent(Guid.NewGuid().ToString(), $"Echoing: {prompt.Trim()}");
        await Task.Delay(50);
        yield return new ExecCommandBeginEvent(Guid.NewGuid().ToString(), new[]{"touch","file.txt"}, "/tmp");
        await Task.Delay(50);
        yield return new ExecCommandEndEvent(Guid.NewGuid().ToString(), "", "", 0);
        await Task.Delay(50);
        yield return new McpToolCallBeginEvent(Guid.NewGuid().ToString(), "server", "tool", "{\"city\":\"sf\"}");
        await Task.Delay(50);
        yield return new McpToolCallEndEvent(Guid.NewGuid().ToString(), true, "{\"result\":42}");
        await Task.Delay(50);
        var changes = new Dictionary<string,string>{{"file.txt","+hello"}};
        yield return new PatchApplyBeginEvent(Guid.NewGuid().ToString(), true, changes);
        await Task.Delay(50);
        yield return new PatchApplyEndEvent(Guid.NewGuid().ToString(), "patched", "", true);
        await Task.Delay(50);
        yield return new ExecApprovalRequestEvent(Guid.NewGuid().ToString(), new[]{"rm","-rf","/"});
        await Task.Delay(50);
        yield return new PatchApplyApprovalRequestEvent(Guid.NewGuid().ToString(), "patch diff");
        await Task.Delay(50);
        yield return new AgentReasoningEvent(Guid.NewGuid().ToString(), "thinking...");
        await Task.Delay(50);
        yield return new TaskCompleteEvent(Guid.NewGuid().ToString(), $"{prompt.Trim()} done");
    }
}
