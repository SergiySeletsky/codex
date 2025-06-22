using CodexCli.Util;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

public class CodexGetWritableRootsTests
{
    [Fact]
    public void IncludesCwd()
    {
        var cwd = "/tmp";
        var roots = Codex.GetWritableRoots(cwd);
        Assert.Contains(cwd, roots);
    }

    [Fact]
    public void MacIncludesTempDir()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
        var cwd = "/tmp";
        var roots = Codex.GetWritableRoots(cwd);
        Assert.Contains(Path.GetTempPath(), roots);
    }
}
