using CodexCli.Util;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public class CodexMaybeNotifyTests
{
    [Fact]
    public async Task MaybeNotify_SpawnsCommand()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var cmd = new List<string>{"sh", "-c", $"echo hi > \"{tmp}\""};
        Codex.MaybeNotify(cmd, new AgentTurnCompleteNotification("1", new string[0], "done"));
        await Task.Delay(200);
        Assert.True(File.Exists(tmp));
        Assert.Equal("hi\n", await File.ReadAllTextAsync(tmp));
    }

    [Fact]
    public void MaybeNotify_NoCommandDoesNothing()
    {
        Codex.MaybeNotify(null, new AgentTurnCompleteNotification("2", new string[0], "done"));
        Codex.MaybeNotify(new List<string>(), new AgentTurnCompleteNotification("3", new string[0], "done"));
    }
}

