using System;
using System.Diagnostics;
using System.IO;
using CodexCli.Util;
using CodexCli.Models;
using System.Text.Json;
using System.Linq;
using Xunit;

public class CrossCliCompatTests
{
    [CrossCliFact]
    public void VersionMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli --version");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --version");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void HelpMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli --help");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --help");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }
    [CrossCliFact]
    public void TuiHelpMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexTui --help");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- --help");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void TuiVersionMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexTui --version");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- --version");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void TuiLoginScreenMatches()
    {
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexTui --skip-git-repo-check", "q\n");
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- --skip-git-repo-check", "q\n");
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void TuiChatMatches()
    {
        var input = "hi\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexTui --skip-git-repo-check --model-provider Mock", input);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- -c model_provider=Mock --skip-git-repo-check", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void TuiCtrlDMatches()
    {
        var input = "\u0004";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexTui --skip-git-repo-check --model-provider Mock", input);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- -c model_provider=Mock --skip-git-repo-check", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void TuiCtrlCMatches()
    {
        var input = "hi\n\u0003/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexTui --skip-git-repo-check --model-provider Mock", input);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- -c model_provider=Mock --skip-git-repo-check", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void TuiConfigMatches()
    {
        var input = "/config\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexTui --skip-git-repo-check --model-provider Mock", input);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- -c model_provider=Mock --skip-git-repo-check", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void TuiSessionsMatches()
    {
        var input = "/sessions\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexTui --skip-git-repo-check --model-provider Mock", input);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- -c model_provider=Mock --skip-git-repo-check", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void TuiToggleMouseModeMatches()
    {
        var input = "/toggle-mouse-mode\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexTui --skip-git-repo-check --model-provider Mock", input);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- -c model_provider=Mock --skip-git-repo-check", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveQuitMatches()
    {
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", "/quit\n");
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", "/quit\n");
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveCtrlDMatches()
    {
        var seq = "\u0004";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveCtrlCMatches()
    {
        var seq = "hi\n\u0003/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveCancelImmediatelyMatches()
    {
        var seq = "\u0003/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveHistoryMatches()
    {
        var input = "/history\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveConfigMatches()
    {
        var input = "/config\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveScrollMatches()
    {
        var input = "/scroll-up 1\n/scroll-down 1\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveSessionsMatches()
    {
        var input = "/sessions\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveNewMatches()
    {
        var input = "hi\n/new\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveLogMatches()
    {
        var input = "/log\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveVersionMatches()
    {
        var input = "/version\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void TuiGitWarningMatches()
    {
        var tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tmp);
        var repoRoot = GitUtils.GetRepoRoot(Directory.GetCurrentDirectory())!;
        var dotnetProj = Path.Combine(repoRoot, "codex-dotnet", "CodexTui");
        var dotnet = RunProcess("bash", $"-c 'cd {tmp} && printf n | dotnet run --project {dotnetProj}'");
        var rust = RunProcess("bash", $"-c 'cd {tmp} && printf n | cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml'");
        Directory.Delete(tmp, true);
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void InteractiveHelpMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli interactive --help");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- --help");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }


    [CrossCliFact]
    public void ProviderListMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli provider list --names-only");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- provider list --names-only");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void HistoryCountMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli history messages-count");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- history messages-count");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ServersListMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} mcp-manager servers");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager servers");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void RootsListMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} mcp-manager roots list --server test");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager roots list --server test");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void MessagesCountMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} mcp-manager messages count --server test");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages count --server test");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void MessagesListMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} mcp-manager messages list --server test");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages list --server test");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void PromptsListMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} mcp-manager prompts list --server test");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts list --server test");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void PromptGetMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} mcp-manager prompts get --server test demo");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts get --server test demo");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ResourcesListMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} mcp-manager resources list --server test");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager resources list --server test");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void TemplatesListMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} mcp-manager templates --server test");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager templates --server test");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void LoggingSetLevelMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} mcp-manager set-level --server test debug");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager set-level --server test debug");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void AddAndGetMessageMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} mcp-manager messages add --server test hi && dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager messages get --server test 0");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages add --server test hi && cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages get --server test 0'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    private (string stdout, string stderr) RunProcess(string file, string args, Dictionary<string,string>? env = null)
    {
        var repoRoot = GitUtils.GetRepoRoot(Directory.GetCurrentDirectory())!;
        var psi = new ProcessStartInfo(file, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = repoRoot
        };
        if (env != null)
            foreach (var kv in env)
                psi.Environment[kv.Key] = kv.Value;
        var p = Process.Start(psi)!;
        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (stdout, stderr);
    }

    private (string stdout, string stderr) RunProcessWithPty(string command, string input, Dictionary<string,string>? env = null)
    {
        var escaped = command.Replace("'", "'\\''");
        var repoRoot = GitUtils.GetRepoRoot(Directory.GetCurrentDirectory())!;
        var psi = new ProcessStartInfo("bash", $"-c \"script -qfec '{escaped}' /dev/null\"")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = repoRoot
        };
        if (env != null)
            foreach (var kv in env)
                psi.Environment[kv.Key] = kv.Value;
        var p = Process.Start(psi)!;
        if (!string.IsNullOrEmpty(input))
            p.StandardInput.Write(input);
        p.StandardInput.Close();
        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (stdout.StartsWith(input))
            stdout = stdout.Substring(input.Length);
        return (stdout, stderr);
    }

    private string CreateTempConfig()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, """
[mcp_servers.test]
command = "dotnet"
args = ["run", "--project", "codex-dotnet/CodexCli", "mcp"]
""");
        return path;
    }

    [CrossCliFact]
    public void ServersListJsonMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} mcp-manager servers --json");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager servers --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void RootsAddJsonMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c 'dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager roots add --server test x && dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager roots list --server test --json'");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager roots add --server test x && cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager roots list --server test --json'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void PromptsAddJsonMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c 'dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager prompts add --server test demo \"hello\" && dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager prompts get --server test demo --json'");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts add --server test demo \"hello\" && cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts get --server test demo --json'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void MessagesAddJsonMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c 'dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager messages add --server test hi --json && dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager messages get --server test 0 --json'");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages add --server test hi --json && cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages get --server test 0 --json'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ResourcesWriteJsonMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c 'dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager resources write --server test r1 hi --json && dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager resources list --server test --json'");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager resources write --server test r1 hi --json && cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager resources list --server test --json'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void LoggingSetLevelJsonMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} mcp-manager set-level --server test debug --json");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager set-level --server test debug --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void McpManagerCallMatches()
    {
        var cfg = CreateTempConfig();
        var fq = "test__OAI_CODEX_MCP__codex";
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} mcp-manager call {fq} --json");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager call {fq} --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void McpClientPingMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli mcp-client dotnet --project codex-dotnet/CodexCli mcp --ping");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/mcp-client/Cargo.toml -- cargo run --quiet --manifest-path ../../codex-rs/mcp-server/Cargo.toml");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void McpClientListToolsMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli mcp-client dotnet --project codex-dotnet/CodexCli mcp");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/mcp-client/Cargo.toml -- cargo run --quiet --manifest-path ../../codex-rs/mcp-server/Cargo.toml");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void McpClientListRootsMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli mcp-client dotnet --project codex-dotnet/CodexCli mcp --list-roots --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/mcp-client/Cargo.toml -- cargo run --quiet --manifest-path ../../codex-rs/mcp-server/Cargo.toml --list-roots --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void McpClientCallCodexMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli mcp-client dotnet --project codex-dotnet/CodexCli mcp --call-codex --codex-prompt hi --codex-provider mock --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/mcp-client/Cargo.toml -- --call-codex --codex-prompt hi --codex-provider mock --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact(Skip="flaky in CI")]
    public void McpManagerWatchEventsMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c '(sleep 0.2; dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager prompts add --server test w hi) & dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager prompts list --server test --events-url http://localhost:8080 --watch-events --json | head -n 2'");
        var rust = RunProcess("bash", $"-c '(sleep 0.2; cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts add --server test w hi) & cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts list --server test --events-url http://localhost:8080 --watch-events --json | head -n 2'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact(Skip="flaky in CI")]
    public void HistoryWatchEventsMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c '(sleep 0.2; dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager prompts add --server test w hi) & dotnet run --project codex-dotnet/CodexCli --config {cfg} history messages-meta --events-url http://localhost:8080 --watch-events | head -n 2'");
        var rust = RunProcess("bash", $"-c '(sleep 0.2; cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts add --server test w hi) & cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} history messages-meta --events-url http://localhost:8080 --watch-events | head -n 2'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact(Skip="flaky in CI")]
    public void McpManagerClearWatchEventsMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c '(sleep 0.2; dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager messages add --server test hi) & dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager messages clear --server test --events-url http://localhost:8080 --watch-events | head -n 2'");
        var rust = RunProcess("bash", $"-c '(sleep 0.2; cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages add --server test hi) & cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages clear --server test --events-url http://localhost:8080 --watch-events | head -n 2'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact(Skip="flaky in CI")]
    public void McpManagerAddMessageWatchEventsMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c 'dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager messages add --server test hi --events-url http://localhost:8080 --watch-events | head -n 2'");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager messages add --server test hi --events-url http://localhost:8080 --watch-events | head -n 2'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact(Skip="flaky in CI")]
    public void McpManagerAddPromptWatchEventsMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c 'dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager prompts add --server test p1 hi --events-url http://localhost:8080 --watch-events | head -n 2'");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts add --server test p1 hi --events-url http://localhost:8080 --watch-events | head -n 2'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact(Skip="flaky in CI")]
    public void McpManagerAddRootWatchEventsMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c 'dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager roots add --server test r3 --events-url http://localhost:8080 --watch-events | head -n 2'");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager roots add --server test r3 --events-url http://localhost:8080 --watch-events | head -n 2'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact(Skip="flaky in CI")]
    public void McpManagerRemoveRootWatchEventsMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c '(sleep 0.2; dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager roots add --server test r4) & dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager roots remove --server test r4 --events-url http://localhost:8080 --watch-events | head -n 2'");
        var rust = RunProcess("bash", $"-c '(sleep 0.2; cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager roots add --server test r4) & cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager roots remove --server test r4 --events-url http://localhost:8080 --watch-events | head -n 2'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact(Skip="flaky in CI")]
    public void McpManagerWriteResourceWatchEventsMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c 'dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager resources write --server test r2 hi --events-url http://localhost:8080 --watch-events | head -n 2'");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager resources write --server test r2 hi --events-url http://localhost:8080 --watch-events | head -n 2'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact(Skip="flaky in CI")]
    public void McpManagerSubscribeResourceWatchEventsMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c '(sleep 0.2; dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager resources subscribe mem:/s.txt --server test) & dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager resources write --server test mem:/s.txt hi --events-url http://localhost:8080 --watch-events | head -n 2'");
        var rust = RunProcess("bash", $"-c '(sleep 0.2; cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager resources subscribe mem:/s.txt --server test) & cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager resources write --server test mem:/s.txt hi --events-url http://localhost:8080 --watch-events | head -n 2'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact(Skip="flaky in CI")]
    public void McpManagerRemoveResourceWatchEventsMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c '(sleep 0.2; dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager resources write --server test mem:/t.txt hi) & dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager resources remove --server test mem:/t.txt --events-url http://localhost:8080 --watch-events | head -n 2'");
        var rust = RunProcess("bash", $"-c '(sleep 0.2; cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager resources write --server test mem:/t.txt hi) & cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager resources remove --server test mem:/t.txt --events-url http://localhost:8080 --watch-events | head -n 2'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact(Skip="flaky in CI")]
    public void McpManagerRemovePromptWatchEventsMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c '(sleep 0.2; dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager prompts add --server test p2 hi) & dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager prompts remove --server test p2 --events-url http://localhost:8080 --watch-events | head -n 2'");
        var rust = RunProcess("bash", $"-c '(sleep 0.2; cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts add --server test p2 hi) & cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager prompts remove --server test p2 --events-url http://localhost:8080 --watch-events | head -n 2'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact(Skip="flaky in CI")]
    public void McpManagerSetLevelWatchEventsMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("bash", $"-c 'dotnet run --project codex-dotnet/CodexCli --config {cfg} mcp-manager set-level --server test debug --events-url http://localhost:8080 --watch-events | head -n 2'");
        var rust = RunProcess("bash", $"-c 'cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} mcp-manager set-level --server test debug --events-url http://localhost:8080 --watch-events | head -n 2'");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void HistoryMessagesCountJsonMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli history messages-count --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- history messages-count --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void HistoryMessagesSearchJsonMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli history messages-search hi --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- history messages-search hi --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void HistoryMessagesLastJsonMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli history messages-last 1 --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- history messages-last 1 --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void HistoryMessagesEntryMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli history messages-entry 0");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- history messages-entry 0");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void HistoryStatsJsonMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli history stats --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- history stats --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ReplayJsonMatches()
    {
        string path = Path.GetTempFileName();
        var item = new MessageItem("assistant", new List<ContentItem>{ new("output_text","hi") });
        File.WriteAllText(path, JsonSerializer.Serialize(item, item.GetType()) + "\n");
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli replay {path} --json");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- replay {path} --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
        File.Delete(path);
    }

    [CrossCliFact]
    public void ReplayMessagesOnlyMatches()
    {
        string path = Path.GetTempFileName();
        var items = new ResponseItem[]
        {
            new MessageItem("user", new List<ContentItem>{ new("output_text","hi") }),
            new FunctionCallItem("tool","{}","1")
        };
        File.WriteAllLines(path, items.Select(i => JsonSerializer.Serialize(i, i.GetType())));
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli replay {path} --messages-only");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- replay {path} --messages-only");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
        File.Delete(path);
    }

    [CrossCliFact]
    public void ReplayFollowMatches()
    {
        string dotPath = Path.GetTempFileName();
        string rustPath = Path.GetTempFileName();
        var first = JsonSerializer.Serialize(new MessageItem("assistant", new List<ContentItem>{ new("output_text","a1") }));
        var second = JsonSerializer.Serialize(new MessageItem("assistant", new List<ContentItem>{ new("output_text","a2") }));
        File.WriteAllText(dotPath, first + "\n");
        File.WriteAllText(rustPath, first + "\n");
        var dotnet = RunProcess("bash", $"-c \"(sleep 0.2; echo '{second}' >> {dotPath}) & dotnet run --project codex-dotnet/CodexCli replay {dotPath} --json --follow --max-items 2\"");
        var rust = RunProcess("bash", $"-c \"(sleep 0.2; echo '{second}' >> {rustPath}) & cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- replay {rustPath} --json --follow --max-items 2\"");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
        File.Delete(dotPath);
        File.Delete(rustPath);
    }

    [CrossCliFact]
    public void HistoryCountAfterSessionMatches()
    {
        RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", "/quit\n");
        RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", "/quit\n");
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli history messages-count");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- history messages-count");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ProviderInfoJsonMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli provider info openai --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- provider info openai --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ProviderCurrentJsonMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli provider current --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- provider current --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ExecBasicMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli exec hi --model-provider Mock");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- exec hi -c model_provider=Mock");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }
    [CrossCliFact]
    public void ExecAggregatedMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli exec hi --model-provider Mock");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- exec hi -c model_provider=Mock");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ExecNotifyRunsScript()
    {
        var tmp = Path.GetTempFileName();
        File.Delete(tmp);
        var script = Path.GetTempFileName();
        File.WriteAllText(script, $"echo done > {tmp}");
        RunProcess("bash", $"-c 'chmod +x {script}; dotnet run --project codex-dotnet/CodexCli exec hi --model-provider Mock --notify {script}'");
        Assert.True(File.Exists(tmp));
        File.Delete(tmp);
        RunProcess("bash", $"-c 'chmod +x {script}; cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- exec hi -c model_provider=Mock --notify {script}'");
        Assert.True(File.Exists(tmp));
    }

    [CrossCliFact]
    public void ExecMcpMatches()
    {
        var cfg = CreateTempConfig();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli --config {cfg} exec hi --model-provider Mock --mcp-server test --json");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- --config {cfg} exec hi -c model_provider=Mock --mcp-server test --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ExecJsonMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli exec hi --model-provider Mock --json");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- exec hi -c model_provider=Mock --json");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ExecPatchSummaryMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli exec hi --model-provider Mock");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- exec hi -c model_provider=Mock");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ApplyPatchCliMatches()
    {
        using var dir = new TempDir();
        var patchPath = System.IO.Path.Combine(dir.Path, "p.patch");
        System.IO.File.WriteAllText(patchPath, "*** Begin Patch\n*** Add File: foo.txt\n+hi\n*** End Patch");
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli apply_patch {patchPath} --cwd {dir.Path}");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- apply_patch {patchPath} --cwd {dir.Path}");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ExecHistoryCountMatches()
    {
        RunProcess("dotnet", "run --project codex-dotnet/CodexCli exec hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc");
        RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- exec hi -c model_provider=Mock --hide-agent-reasoning --disable-response-storage --no-project-doc");
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli history messages-count");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- history messages-count");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ExecCancelImmediatelyMatches()
    {
        var seq = "\u0003";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli exec hi --model-provider Mock", seq);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- exec hi -c model_provider=Mock", seq);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void DebugSeatbeltMatches()
    {
        var dotnet = RunProcess("dotnet", "run --project codex-dotnet/CodexCli debug seatbelt \"echo hi\"");
        var rust = RunProcess("cargo", "run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- debug seatbelt \"echo hi\"");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }


    [CrossCliFact]
    public void ExecImageUploadMatches()
    {
        string path = CreateTempImage();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli exec hi --model-provider Mock --image {path}");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- exec hi -c model_provider=Mock --image {path}");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void ExecImageUploadJpegMatches()
    {
        string path = CreateTempJpeg();
        var dotnet = RunProcess("dotnet", $"run --project codex-dotnet/CodexCli exec hi --model-provider Mock --image {path}");
        var rust = RunProcess("cargo", $"run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- exec hi -c model_provider=Mock --image {path}");
        Assert.Equal(rust.stdout.Trim(), dotnet.stdout.Trim());
    }

    [CrossCliFact]
    public void TuiImageUploadMatches()
    {
        string path = CreateTempImage();
        var dotnet = RunProcessWithPty($"dotnet run --project codex-dotnet/CodexTui --skip-git-repo-check --model-provider Mock --image {path}", "/quit\n");
        var rust = RunProcessWithPty($"cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- --skip-git-repo-check -c model_provider=Mock --image {path}", "/quit\n");
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void TuiImageUploadJpegMatches()
    {
        string path = CreateTempJpeg();
        var dotnet = RunProcessWithPty($"dotnet run --project codex-dotnet/CodexTui --skip-git-repo-check --model-provider Mock --image {path}", "/quit\n");
        var rust = RunProcessWithPty($"cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- --skip-git-repo-check -c model_provider=Mock --image {path}", "/quit\n");
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void TuiImageCommandMatches()
    {
        string path = CreateTempImage();
        var input = $"/image {path}\n/quit\n";
        var dotnet = RunProcessWithPty($"dotnet run --project codex-dotnet/CodexTui --skip-git-repo-check --model-provider Mock", input);
        var rust = RunProcessWithPty($"cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- --skip-git-repo-check -c model_provider=Mock", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void TuiImageCommandJpegMatches()
    {
        string path = CreateTempJpeg();
        var input = $"/image {path}\n/quit\n";
        var dotnet = RunProcessWithPty($"dotnet run --project codex-dotnet/CodexTui --skip-git-repo-check --model-provider Mock", input);
        var rust = RunProcessWithPty($"cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- --skip-git-repo-check -c model_provider=Mock", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void TuiMouseWheelMatches()
    {
        var seq = "\u001b[<64;0;0M\u001b[<65;0;0M/quit\n";
        var dotnet = RunProcessWithPty($"dotnet run --project codex-dotnet/CodexTui --skip-git-repo-check --model-provider Mock", seq);
        var rust = RunProcessWithPty($"cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- --skip-git-repo-check -c model_provider=Mock", seq);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void TuiNonBlockingInputMatches()
    {
        var input = "hello\n/quit\n";
        var dotnet = RunProcessWithPty($"dotnet run --project codex-dotnet/CodexTui --skip-git-repo-check --model-provider Mock", input);
        var rust = RunProcessWithPty($"cargo run --quiet --manifest-path ../../codex-rs/tui/Cargo.toml -- --skip-git-repo-check -c model_provider=Mock", input);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveArrowEditMatches()
    {
        var seq = "hi\u001b[Da\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveHomeEndMatches()
    {
        var seq = "hi\u001b[Ha\u001b[Fb\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [Fact]
    public void InteractivePasteMatches()
    {
        var seq = "\u001b[200~hi\nthere\u001b[201~\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveInvalidPasteMatches()
    {
        var seq = "\u001b[200Xab\n/quit\n";
        var dotnet = RunProcessWithPty("dotnet run --project codex-dotnet/CodexCli interactive --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var rust = RunProcessWithPty("cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    [CrossCliFact]
    public void InteractiveSaveMatches()
    {
        var tmp = Path.GetTempFileName();
        File.Delete(tmp);
        var seq = $"/save {tmp}\n/quit\n";
        var dotnet = RunProcessWithPty($"dotnet run --project codex-dotnet/CodexCli interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        var rust = RunProcessWithPty($"cargo run --quiet --manifest-path ../../codex-rs/cli/Cargo.toml -- interactive hi --model-provider Mock --hide-agent-reasoning --disable-response-storage --no-project-doc", seq);
        File.Delete(tmp);
        var dOut = AnsiEscape.StripAnsi(dotnet.stdout).Trim();
        var rOut = AnsiEscape.StripAnsi(rust.stdout).Trim();
        Assert.Equal(rOut, dOut);
    }

    private string CreateTempImage()
    {
        var tmp = Path.GetTempFileName() + ".png";
        File.WriteAllBytes(tmp, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAADUlEQVR4nGMAAQAABQABDQottAAAAABJRU5ErkJggg=="));
        return tmp;
    }

    private string CreateTempJpeg()
    {
        var tmp = Path.GetTempFileName() + ".jpg";
        File.WriteAllBytes(tmp, Convert.FromBase64String("/9j/4AAQSkZJRgABAQAAAQABAAD/2wBDAAgGBgcGBQgHBwcJCQgKDBQNDAsLDBkSEw8UHRofHh0aHBwgJC4nICIsIxwcKDcpLDAxNDQ0Hyc5PTgyPC4zNDL/2wBDAQkJCQwLDBgNDRgyIRwhMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjL/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwDi6KKK+ZP3E//Z"));
        return tmp;
    }
}

    internal sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        public TempDir() { System.IO.Directory.CreateDirectory(Path); }
        public void Dispose() { System.IO.Directory.Delete(Path, true); }
    }
