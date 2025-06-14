using CodexCli.ApplyPatch;
using Xunit;

public class UnifiedDiffTests
{
    [Fact]
    public void ParseUnifiedIgnoresHeaders()
    {
        string diff = "--- a.txt\n+++ b.txt\n@@\n-line1\n+line2";
        var lines = PatchParser.ParseUnified(diff);
        Assert.Contains("-line1", lines);
        Assert.Contains("+line2", lines);
    }
}
