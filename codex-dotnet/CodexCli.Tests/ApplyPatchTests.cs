using CodexCli.ApplyPatch;
using Xunit;

public class ApplyPatchTests
{
    [Fact]
    public void Parse_Add_Delete_Update()
    {
        string patch = "*** Begin Patch\n*** Add File: a.txt\n+hi\n*** Delete File: b.txt\n*** Update File: c.txt\n context\n-line1\n+line2\n*** End Patch";
        var hunks = PatchParser.Parse(patch);
        Assert.Equal(3, hunks.Count);
        Assert.IsType<AddFileHunk>(hunks[0]);
        Assert.IsType<DeleteFileHunk>(hunks[1]);
        Assert.IsType<UpdateFileHunk>(hunks[2]);
    }

    [Fact]
    public void Apply_AddFile()
    {
        using var dir = new TempDir();
        string patch = "*** Begin Patch\n*** Add File: foo.txt\n+hello\n*** End Patch";
        var output = PatchApplier.Apply(patch, dir.Path);
        Assert.True(File.Exists(Path.Combine(dir.Path, "foo.txt")));
        var text = File.ReadAllText(Path.Combine(dir.Path, "foo.txt"));
        Assert.Equal("hello\n", text);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        public TempDir() { Directory.CreateDirectory(Path); }
        public void Dispose() { Directory.Delete(Path, true); }
    }
}
