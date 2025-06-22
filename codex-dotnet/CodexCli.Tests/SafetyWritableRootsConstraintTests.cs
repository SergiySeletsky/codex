using CodexCli.ApplyPatch;
using CodexCli.Util;
using System.Collections.Generic;
using System.IO;
using Xunit;

public class SafetyWritableRootsConstraintTests
{
    [Fact]
    public void WritableRootsConstraint()
    {
        var cwd = Directory.GetCurrentDirectory();
        var parent = Directory.GetParent(cwd)!.FullName;
        ApplyPatchAction MakeAdd(string p) => new(new Dictionary<string, ApplyPatchFileChange>
        {
            [p] = new ApplyPatchFileChange { Kind = "add", Content = "" }
        });

        var addInside = MakeAdd(Path.Combine(cwd, "inner.txt"));
        var addOutside = MakeAdd(Path.Combine(parent, "outside.txt"));
        var addOutside2 = MakeAdd(Path.Combine(parent, "outside.txt"));

        Assert.True(Safety.IsWritePatchConstrainedToWritableRoots(addInside, new List<string>{"."}, cwd));
        Assert.False(Safety.IsWritePatchConstrainedToWritableRoots(addOutside2, new List<string>{"."}, cwd));
        Assert.True(Safety.IsWritePatchConstrainedToWritableRoots(addOutside, new List<string>{".."}, cwd));
    }
}
