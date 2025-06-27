using CodexCli.Util;
using CodexCli.Models;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class CodexStatePartialCloneTests
{
    [Fact]
    public void ClonesApprovedCommandsAndPreviousResponseId()
    {
        var state = new CodexState();
        state.ApprovedCommands.Add(new List<string>{"echo","hi"});
        state.PreviousResponseId = "abc";
        var clone = state.PartialClone(false);
        Assert.Equal(state.PreviousResponseId, clone.PreviousResponseId);
        Assert.Single(clone.ApprovedCommands);
        Assert.NotSame(state.ApprovedCommands, clone.ApprovedCommands);
        Assert.Equal(state.ApprovedCommands.First(), clone.ApprovedCommands.First());
        Assert.Null(clone.ZdrTranscript);
    }

    [Fact]
    public void OptionallyClonesTranscript()
    {
        var state = new CodexState();
        var hist = new ConversationHistory();
        hist.RecordItems(new[]{ new MessageItem("assistant", new List<ContentItem>{ new("output_text", "hi") }) });
        state.ZdrTranscript = hist;
        var clone = state.PartialClone(true);
        Assert.NotNull(clone.ZdrTranscript);
        Assert.Equal(hist.Contents().Count, clone.ZdrTranscript!.Contents().Count);
    }

    [Fact]
    public void ClonesWritableRoots()
    {
        var state = new CodexState();
        state.WritableRoots.Add("/tmp");
        var clone = state.PartialClone(false);
        Assert.Equal(state.WritableRoots, clone.WritableRoots);
        Assert.NotSame(state.WritableRoots, clone.WritableRoots);
    }
}
