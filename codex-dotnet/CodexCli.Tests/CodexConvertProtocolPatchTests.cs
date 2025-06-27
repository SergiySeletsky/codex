using CodexCli.Protocol;
using CodexCli.Util;
using CodexCli.ApplyPatch;
using System.Collections.Generic;
using Xunit;

public class CodexConvertProtocolPatchTests
{
    [Fact]
    public void ConvertsProtocolPatchToAction()
    {
        var changes = new Dictionary<string,FileChange>
        {
            ["foo.txt"] = new AddFileChange("hi"),
            ["bar.txt"] = new DeleteFileChange(),
            ["baz.txt"] = new UpdateFileChange("+new\n", "dest.txt")
        };
        var action = Codex.ConvertProtocolPatchToAction(changes);
        Assert.Equal(3, action.Changes.Count);
        Assert.Equal("add", action.Changes["foo.txt"].Kind);
        Assert.Equal("hi", action.Changes["foo.txt"].Content);
        Assert.Equal("delete", action.Changes["bar.txt"].Kind);
        Assert.Equal("update", action.Changes["baz.txt"].Kind);
        Assert.Equal("+new\n", action.Changes["baz.txt"].UnifiedDiff);
        Assert.Equal("dest.txt", action.Changes["baz.txt"].MovePath);
    }
}
