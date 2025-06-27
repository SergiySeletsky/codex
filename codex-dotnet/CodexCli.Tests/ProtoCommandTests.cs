using CodexCli.Commands;
using CodexCli.Util;
using System.Diagnostics;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using Xunit;

public class ProtoCommandTests
{
    [Fact(Skip="spawns CLI")] 
    public async Task PrintsMethod()
    {
        var repo = GitUtils.GetRepoRoot(Directory.GetCurrentDirectory())!;
        var psi = new ProcessStartInfo("dotnet", "run --project codex-dotnet/CodexCli proto")
        {
            WorkingDirectory = repo,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        var p = Process.Start(psi)!;
        await p.StandardInput.WriteAsync("{\"method\":\"foo\"}\n");
        p.StandardInput.Close();
        var output = await p.StandardOutput.ReadToEndAsync();
        p.WaitForExit();
        Assert.Contains("method=foo", output);
    }
}
