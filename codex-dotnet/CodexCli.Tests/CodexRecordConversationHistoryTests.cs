using CodexCli.Config;
using CodexCli.Util;
using Xunit;

public class CodexRecordConversationHistoryTests
{
    [Fact]
    public void DefaultsBasedOnWireApi()
    {
        Assert.False(Codex.RecordConversationHistory(false, WireApi.Responses));
        Assert.True(Codex.RecordConversationHistory(false, WireApi.Chat));
    }

    [Fact]
    public void DisableOverrides()
    {
        Assert.True(Codex.RecordConversationHistory(true, WireApi.Responses));
    }
}
