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

    [Fact(Skip="requires rust toolchain")]
    public void ServersListJsonMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager servers --json");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager servers --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void RootsAddJsonMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c 'dotnet run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager roots add --server test x && dotnet run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager roots list --server test --json'");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager roots add --server test x && cargo run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager roots list --server test --json'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void PromptsAddJsonMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c 'dotnet run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager prompts add --server test demo \"hello\" && dotnet run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager prompts get --server test demo --json'");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts add --server test demo \"hello\" && cargo run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts get --server test demo --json'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void MessagesAddJsonMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c 'dotnet run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager messages add --server test hi --json && dotnet run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager messages get --server test 0 --json'");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages add --server test hi --json && cargo run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages get --server test 0 --json'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void ResourcesWriteJsonMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c 'dotnet run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager resources write --server test r1 hi --json && dotnet run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager resources list --server test --json'");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager resources write --server test r1 hi --json && cargo run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager resources list --server test --json'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void LoggingSetLevelJsonMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project ../codex-dotnet/CodexCli --config {cfg} mcp-manager set-level --server test debug --json");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager set-level --server test debug --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void HistoryMessagesCountJsonMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project ../codex-dotnet/CodexCli history messages-count --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- history messages-count --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void HistoryMessagesSearchJsonMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project ../codex-dotnet/CodexCli history messages-search hi --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- history messages-search hi --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void HistoryMessagesLastJsonMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project ../codex-dotnet/CodexCli history messages-last 1 --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- history messages-last 1 --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void HistoryStatsJsonMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project ../codex-dotnet/CodexCli history stats --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- history stats --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void ProviderInfoJsonMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project ../codex-dotnet/CodexCli provider info openai --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- provider info openai --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [Fact(Skip="requires rust toolchain")]
    public void ProviderCurrentJsonMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project ../codex-dotnet/CodexCli provider current --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../codex-rs/cli/Cargo.toml -- provider current --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }
}
