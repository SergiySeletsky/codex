using CodexCli.Models;
using CodexCli.Util;
using Xunit;

public class ExecRunnerTimeoutTests
{
    [Fact(Skip="Requires spawn permissions not available in CI")]
    public async Task CommandTimesOut()
    {
        var p = new ExecParams(new List<string>{"bash","-c","sleep 2"}, Directory.GetCurrentDirectory(), 100, new(), null, null, null);
        await Assert.ThrowsAsync<OperationCanceledException>(() => ExecRunner.RunAsync(p, CancellationToken.None));
    }
}
