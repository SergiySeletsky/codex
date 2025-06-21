using CodexCli.Util;
using CodexCli.Commands;
using CodexCli.Models;
using Xunit;

public class ReasoningUtilsTests
{
    [Fact]
    public void CreateReasoningParam_ReturnsNullWhenEffortNone()
    {
        var r = ReasoningUtils.CreateReasoningParam("codex", ReasoningEffort.None, ReasoningSummary.Brief);
        Assert.Null(r);
    }

    [Fact]
    public void CreateReasoningParam_UnsupportedModelReturnsNull()
    {
        var r = ReasoningUtils.CreateReasoningParam("foo", ReasoningEffort.Low, ReasoningSummary.Brief);
        Assert.Null(r);
    }

    [Fact]
    public void CreateReasoningParam_ReturnsReasoning()
    {
        var r = ReasoningUtils.CreateReasoningParam("codex", ReasoningEffort.High, ReasoningSummary.Detailed);
        Assert.NotNull(r);
        Assert.Equal(OpenAiReasoningEffort.High, r!.Effort);
        Assert.Equal(OpenAiReasoningSummary.Detailed, r!.Summary);
    }
}
