using CodexCli.Util;

public class IsSafeCommandTests
{
    [Fact]
    public void BasicChecks()
    {
        Assert.True(IsSafeCommand.Check(new[]{"echo","hello"}));
        Assert.False(IsSafeCommand.Check(new[]{"rm","-rf","/"}));
        Assert.False(IsSafeCommand.Check(new[]{"sudo","ls"}));
    }
}
