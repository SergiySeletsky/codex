using System.Diagnostics;
using System.IO;
using Xunit;

public class CrossCliCompatTests
{
    [Fact(Skip="requires rust toolchain")]
    public void VersionMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project ../codex-dotnet/CodexCli --version");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --version");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void ProviderListMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project ../codex-dotnet/CodexCli provider list --names-only");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- provider list --names-only");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void HistoryCountMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project ../codex-dotnet/CodexCli history messages-count");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- history messages-count");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void ServersListMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager servers");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager servers");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void RootsListMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager roots list --server test");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager roots list --server test");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    private (string stdout, string stderr) RunProcess(string file, string args)
    {
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        var p = Process.Start(psi)!;
        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (stdout, stderr);
    }

    private string CreateTempConfig()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, """
[mcp_servers.test]
command = "dotnet"
args = ["run", "--project", "../codex-dotnet/CodexCli", "mcp"]
""");
        return path;
    }
}
