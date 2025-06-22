using CodexCli.Util;
using CodexCli.Protocol;
using Xunit;

public class SafetyPlatformSandboxTests
{
    [Fact]
    public void GetPlatformSandbox_ReturnsExpectedValue()
    {
        var sandbox = Safety.GetPlatformSandbox();
#if NET7_0_OR_GREATER
        if (OperatingSystem.IsLinux())
            Assert.Equal(SandboxType.LinuxSeccomp, sandbox);
        else if (OperatingSystem.IsMacOS())
            Assert.Equal(SandboxType.MacosSeatbelt, sandbox);
        else
            Assert.Null(sandbox);
#else
        // Simplified assumption for test environments
        Assert.NotNull(sandbox);
#endif
    }
}

