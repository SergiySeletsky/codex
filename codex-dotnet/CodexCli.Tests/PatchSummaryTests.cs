using CodexCli.ApplyPatch;
using Xunit;
using System.IO;
using System.Collections.Generic;

public class PatchSummaryTests
{
    [Fact]
    public void WritesSummary()
    {
        var affected = new AffectedPaths(
            new List<string>{"a.txt"},
            new List<string>{"b.txt"},
            new List<string>{"c.txt"});
        var sw = new StringWriter();
        PatchSummary.PrintSummary(affected, sw);
        var text = sw.ToString();
        Assert.Contains("Success. Updated the following files:", text);
        Assert.Contains("A a.txt", text);
        Assert.Contains("M b.txt", text);
        Assert.Contains("D c.txt", text);
    }
}
