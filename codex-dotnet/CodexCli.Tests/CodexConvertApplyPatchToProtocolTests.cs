using CodexCli.ApplyPatch;
using CodexCli.Protocol;
using CodexCli.Util;
using System.Collections.Generic;
using Xunit;

public class CodexConvertApplyPatchToProtocolTests
{
    [Fact]
    public void ConvertsChanges()
    {
        var action = new ApplyPatchAction(new Dictionary<string, ApplyPatchFileChange>
        {
            ["/tmp/a.txt"] = new ApplyPatchFileChange { Kind = "add", Content = "hi\n" },
            ["/tmp/b.txt"] = new ApplyPatchFileChange { Kind = "delete" },
            ["/tmp/c.txt"] = new ApplyPatchFileChange { Kind = "update", UnifiedDiff = "+hi\n-context\n", MovePath = null }
        });
        var result = Codex.ConvertApplyPatchToProtocol(action);
        Assert.IsType<AddFileChange>(result["/tmp/a.txt"]);
        Assert.IsType<DeleteFileChange>(result["/tmp/b.txt"]);
        Assert.IsType<UpdateFileChange>(result["/tmp/c.txt"]);
        var upd = (UpdateFileChange)result["/tmp/c.txt"];
        Assert.Equal("+hi\n-context\n", upd.UnifiedDiff);
    }

    [Fact]
    public void ParsesAndConverts()
    {
        var argv = new [] { "apply_patch", "*** Begin Patch\n*** Add File: a.txt\n+hi\n*** End Patch" };
        var res = ApplyPatchCommandParser.MaybeParseApplyPatchVerified(argv, "/tmp", out var action);
        Assert.Equal(MaybeApplyPatchVerified.Body, res);
        Assert.NotNull(action);
        var dict = Codex.ConvertApplyPatchToProtocol(action!);
        Assert.Single(dict);
        Assert.Contains("/tmp/a.txt", dict.Keys);
    }
}
