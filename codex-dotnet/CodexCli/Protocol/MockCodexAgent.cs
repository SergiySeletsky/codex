using CodexCli.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CodexCli.Protocol;

public static class MockCodexAgent
{
public static async IAsyncEnumerable<Event> RunAsync(string prompt, IReadOnlyList<string> images,
    Func<Event, Task<ReviewDecision>>? approvalResponder = null)
{
    await Task.Delay(50);
    foreach (var img in images)
    {
        yield return new BackgroundEvent(Guid.NewGuid().ToString(), $"uploaded {Path.GetFileName(img)}");
        await Task.Delay(10);
    }
        yield return new SessionConfiguredEvent(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "gpt-4");
        yield return new TaskStartedEvent(Guid.NewGuid().ToString());
        await Task.Delay(50);
        var msgId = Guid.NewGuid().ToString();
        yield return new AgentMessageEvent(msgId, $"Echoing: {prompt.Trim()}");
        yield return new AddToHistoryEvent(Guid.NewGuid().ToString(), $"{prompt.Trim()}");
        await Task.Delay(50);
        yield return new ExecCommandBeginEvent(Guid.NewGuid().ToString(), new[]{"touch","file.txt"}, "/tmp");
        await Task.Delay(50);
        yield return new ExecCommandEndEvent(Guid.NewGuid().ToString(), "", "", 0);
        await Task.Delay(50);
        yield return new McpToolCallBeginEvent(Guid.NewGuid().ToString(), "server", "tool", "{\"city\":\"sf\"}");
        await Task.Delay(50);
        yield return new McpToolCallEndEvent(Guid.NewGuid().ToString(), true, "{\"result\":42}");
        await Task.Delay(50);
        yield return new McpToolCallEndEvent(Guid.NewGuid().ToString(), true,
            "{\"content\":[{\"type\":\"image\",\"data\":\"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAADUlEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg==\"}]}");
        await Task.Delay(50);
        var changes = new Dictionary<string,FileChange>{{"file.txt", new AddFileChange("hello\n")}};
        yield return new PatchApplyBeginEvent(Guid.NewGuid().ToString(), true, changes);
        await Task.Delay(50);
        yield return new PatchApplyEndEvent(Guid.NewGuid().ToString(), "patched", "", true);
        await Task.Delay(50);
        var execReq = new ExecApprovalRequestEvent(Guid.NewGuid().ToString(), new[]{"rm","-rf","/"});
        yield return execReq;
        if (approvalResponder != null)
        {
            var dec = await approvalResponder(execReq);
            yield return new BackgroundEvent(Guid.NewGuid().ToString(), $"exec_approval:{dec}");
        }
        await Task.Delay(50);
        var patchReq = new PatchApplyApprovalRequestEvent(Guid.NewGuid().ToString(), "patch diff");
        yield return patchReq;
        if (approvalResponder != null)
        {
            var dec = await approvalResponder(patchReq);
            yield return new BackgroundEvent(Guid.NewGuid().ToString(), $"patch_approval:{dec}");
        }
        await Task.Delay(50);
        yield return new AgentReasoningEvent(Guid.NewGuid().ToString(), "thinking...");
        await Task.Delay(50);
        yield return new GetHistoryEntryResponseEvent(Guid.NewGuid().ToString(), "session", 0, prompt.Trim());
        yield return new TaskCompleteEvent(Guid.NewGuid().ToString(), $"{prompt.Trim()} done");
    }
}
