using CodexCli.ApplyPatch;
using System.Collections.Generic;
using System.IO;
using Xunit;

public class PatchApplierApplyActionTests
{
    [Fact]
    public void ApplyActionWritesSummary()
    {
        using var dir = new TempDir();
        var path = Path.Combine(dir.Path, "a.txt");
        var action = new ApplyPatchAction(new Dictionary<string, ApplyPatchFileChange>
        {
            [path] = new ApplyPatchFileChange { Kind = "add", Content = "hi" }
        });
        var sw = new StringWriter();
        PatchApplier.ApplyActionAndReport(action, sw, TextWriter.Null);
        Assert.True(File.Exists(path));
        var output = sw.ToString();
        Assert.Contains("Success. Updated the following files:", output);
        Assert.Contains($"A {path}", output);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        public TempDir() { Directory.CreateDirectory(Path); }
        public void Dispose() { Directory.Delete(Path, true); }
    }
}
