using CodexCli.Commands;
using System.CommandLine;
using System.CommandLine.Parsing;
using Xunit;

public class ApplyPatchCommandCliTests
{
    [Fact]
    public async Task PatchCommandAppliesFile()
    {
        using var dir = new TempDir();
        var patchPath = Path.Combine(dir.Path, "p.patch");
        File.WriteAllText(patchPath, "*** Begin Patch\n*** Add File: foo.txt\n+hi\n*** End Patch");
        var root = new RootCommand();
        root.AddCommand(ApplyPatchCommand.Create());
        var parser = new Parser(root);
        var output = new StringWriter();
        Console.SetOut(output);
        await parser.InvokeAsync($"apply_patch {patchPath} --cwd {dir.Path}");
        Assert.True(File.Exists(Path.Combine(dir.Path, "foo.txt")));
        Assert.Contains("added foo.txt", output.ToString());
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        public TempDir() { Directory.CreateDirectory(Path); }
        public void Dispose() { Directory.Delete(Path, true); }
    }
}
