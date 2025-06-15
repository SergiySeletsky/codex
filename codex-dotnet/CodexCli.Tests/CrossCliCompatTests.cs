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

    [Fact(Skip="requires rust toolchain")]
    public void MessagesCountMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager messages count --server test");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages count --server test");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void MessagesListMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager messages list --server test");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages list --server test");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void PromptsListMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager prompts list --server test");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts list --server test");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void PromptGetMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager prompts get --server test demo");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts get --server test demo");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void ResourcesListMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager resources list --server test");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager resources list --server test");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void TemplatesListMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager templates --server test");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager templates --server test");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void LoggingSetLevelMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager set-level --server test debug");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager set-level --server test debug");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void AddAndGetMessageMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager messages add --server test hi && dotnet run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager messages get --server test 0");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages add --server test hi && cargo run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages get --server test 0'");
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
