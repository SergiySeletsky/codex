using CodexCli.ApplyPatch;
using Xunit;
using System.IO;

public class PatchApplierSummaryIntegrationTests
{
    [Fact]
    public void ApplyAndPrintSummary()
    {
        using var dir = new TempDir();
        var patch = "*** Begin Patch\n*** Add File: foo.txt\n+hi\n*** End Patch";
        var result = PatchApplier.ApplyWithSummary(patch, dir.Path);
        using var sw = new StringWriter();
        PatchSummary.PrintSummary(result.Affected, sw);
        var summary = sw.ToString();
        Assert.Contains("A foo.txt", summary);
        Assert.True(File.Exists(Path.Combine(dir.Path, "foo.txt")));
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        public TempDir() { Directory.CreateDirectory(Path); }
        public void Dispose() { Directory.Delete(Path, true); }
    }
}
