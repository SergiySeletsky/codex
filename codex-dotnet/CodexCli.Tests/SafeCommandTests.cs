using System.Collections.Generic;
using CodexCli.Util;
using Xunit;

public class SafeCommandTests
{
    private static List<string> V(params string[] a) => new List<string>(a);

    [Fact]
    public void KnownSafeExamples()
    {
        Assert.True(SafeCommand.IsSafeToCallWithExec(V("ls")));
        Assert.True(SafeCommand.IsSafeToCallWithExec(V("git","status")));
        Assert.True(SafeCommand.IsSafeToCallWithExec(V("sed","-n","1,5p","file.txt")));
        Assert.True(SafeCommand.IsSafeToCallWithExec(V("find",".","-name","file.txt")));
    }

    [Fact]
    public void BashLcSafeExamples()
    {
        Assert.True(SafeCommand.IsKnownSafeCommand(V("bash","-lc","ls")));
        Assert.True(SafeCommand.IsKnownSafeCommand(V("bash","-lc","git status")));
        Assert.True(SafeCommand.IsKnownSafeCommand(V("bash","-lc","grep -R \"Cargo.toml\" -n")));
    }

    [Fact]
    public void BashLcUnsafeExamples()
    {
        Assert.False(SafeCommand.IsKnownSafeCommand(V("bash","-lc","git", "status")));
        Assert.False(SafeCommand.IsKnownSafeCommand(V("bash","-lc","'git status'")));
        Assert.False(SafeCommand.IsKnownSafeCommand(V("bash","-lc","find . -name file.txt -delete")));
    }

    [Fact]
    public void ParseSingleWordOnlyCommand()
    {
        var result = SafeCommand.IsKnownSafeCommand(V("bash","-lc","sed -n '1,5p' file.txt"));
        Assert.True(result);
    }
}
