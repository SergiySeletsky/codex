using CodexCli.ApplyPatch;
using Xunit;

public class ApplyPatchCommandParserTests
{
    [Fact]
    public void ParseLiteralApplyPatch()
    {
        var argv = new[] { "apply_patch", "*** Begin Patch\n*** Add File: a.txt\n+hi\n*** End Patch" };
        var result = ApplyPatchCommandParser.MaybeParseApplyPatchVerified(argv, "/tmp", out var action);
        Assert.Equal(MaybeApplyPatchVerified.Body, result);
        Assert.NotNull(action);
        Assert.True(action!.Changes.ContainsKey("/tmp/a.txt"));
    }

    [Fact]
    public void ParseHeredocApplyPatch()
    {
        var script = "apply_patch <<'EOF'\n*** Begin Patch\n*** Add File: a.txt\n+hi\n*** End Patch\nEOF\n";
        var argv = new[] { "bash", "-lc", script };
        var result = ApplyPatchCommandParser.MaybeParseApplyPatchVerified(argv, "/tmp", out var action);
        Assert.Equal(MaybeApplyPatchVerified.Body, result);
        Assert.NotNull(action);
        Assert.True(action!.Changes.ContainsKey("/tmp/a.txt"));
    }
}
