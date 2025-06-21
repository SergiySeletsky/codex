using CodexCli.Commands;
using CodexCli.Protocol;
using System.IO;
using Xunit;

public class ApprovalModeCliArgTests
{
    [Fact]
    public void ConvertsToApprovalMode()
    {
        Assert.Equal(ApprovalMode.OnFailure, ApprovalModeCliArg.OnFailure.ToApprovalMode());
        Assert.Equal(ApprovalMode.UnlessAllowListed, ApprovalModeCliArg.UnlessAllowListed.ToApprovalMode());
        Assert.Equal(ApprovalMode.Never, ApprovalModeCliArg.Never.ToApprovalMode());
    }

    [Fact]
    public void ParsesSandboxPermissions()
    {
        var perms = SandboxPermissionOption.Parse(new[] { "disk-write-folder=sub" }, "/base");
        Assert.Single(perms);
        Assert.Equal(SandboxPermissionType.DiskWriteFolder, perms[0].Type);
        Assert.Equal(Path.GetFullPath("/base/sub"), perms[0].Path);
    }
}
