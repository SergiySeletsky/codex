using CodexCli.Commands;
using System.IO;
using Xunit;

public class SandboxPermissionParserTests
{
    [Fact]
    public void ParsesSimplePermission()
    {
        var p = SandboxPermissionParser.Parse("disk-full-read-access", "/tmp");
        Assert.Equal(SandboxPermissionType.DiskFullReadAccess, p.Type);
    }

    [Fact]
    public void ParsesDiskWriteFolderRelative()
    {
        var p = SandboxPermissionParser.Parse("disk-write-folder=sub", "/base");
        Assert.Equal(SandboxPermissionType.DiskWriteFolder, p.Type);
        Assert.Equal(Path.GetFullPath("/base/sub"), p.Path);
    }
}
