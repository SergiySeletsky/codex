using CodexCli.ApplyPatch;
using CodexCli.Util;
using System.Collections.Generic;
using Xunit;

public class CodexFirstOffendingPathTests
{
    [Fact]
    public void ReturnsNullWhenAllWritable()
    {
        var action = new ApplyPatchAction(new Dictionary<string, ApplyPatchFileChange>
        {
            ["file.txt"] = new ApplyPatchFileChange { Kind = "add", Content = "" }
        });
        var result = Codex.FirstOffendingPath(action, new List<string>{"/tmp"}, "/tmp");
        Assert.Null(result);
    }

    [Fact]
    public void FindsOffendingPath()
    {
        var action = new ApplyPatchAction(new Dictionary<string, ApplyPatchFileChange>
        {
            ["/etc/passwd"] = new ApplyPatchFileChange { Kind = "delete" }
        });
        var result = Codex.FirstOffendingPath(action, new List<string>{"/tmp"}, "/tmp");
        Assert.Equal("/etc/passwd", result);
    }
}
