using CodexCli.ApplyPatch;
using Xunit;
using System.IO;

public class PatchApplierApplyAndReportTests
{
    [Fact]
    public void ApplyAndReportOutputsSummary()
    {
        using var dir = new TempDir();
        var patch = "*** Begin Patch\n*** Add File: foo.txt\n+hi\n*** End Patch";
        var sw = new StringWriter();
        PatchApplier.ApplyAndReport(patch, dir.Path, sw, TextWriter.Null);
        var output = sw.ToString();
        Assert.True(File.Exists(Path.Combine(dir.Path, "foo.txt")));
        Assert.Contains("A foo.txt", output);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        public TempDir() { Directory.CreateDirectory(Path); }
        public void Dispose() { Directory.Delete(Path, true); }
    }
}
