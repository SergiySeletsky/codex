using CodexCli.Util;
using System.Net;
using Xunit;

public class CodexErrTests
{
    [Fact]
    public void EnvVarErrorFormatsMessage()
    {
        var err = new EnvVarError("API_KEY", "set it");
        Assert.Contains("API_KEY", err.Message);
        Assert.Contains("set it", err.Message);
    }

    [Fact]
    public void UnexpectedStatusFormatsMessage()
    {
        var ex = CodexException.UnexpectedStatus(HttpStatusCode.BadRequest, "oops");
        Assert.Contains("BadRequest", ex.Message);
        Assert.Contains("oops", ex.Message);
    }
}
