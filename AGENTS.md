# Migration Plan: Rust Codex CLI to .NET

## Completed Summary
- Bootstrapped a .NET CLI with configuration loading, sandbox enforcement and interactive mode.
- Ported core utilities, configuration, patch application and conversation history helpers.
- Implemented RealCodexAgent with SSE streaming and rollout recording parity.
- Migrated Exec, Debug, Login, Proto, MCP, MCP manager, MCP client and Replay commands with cross CLI tests.
- Ported Codex tool-runner and tool-call parameter with serialization and integration tests.
- Added spawn helpers, submission loop and approval workflow with corresponding unit tests.
- Added cross CLI tests verifying JSON output, patch summaries, patch approval and network sandbox behaviour.
- Skipped `OpenAiApiKeyTests.ReadsValueFromEnvironment` and `ExecRunnerTimeoutTests.CommandTimesOut` when environment permissions are missing.

## Rust to C# Mapping (selected)
- `cli` commands -> `CodexCli/Commands`
- `core` utilities -> `CodexCli/Util`
- `core` configuration and models -> `CodexCli/Config` and `CodexCli/Models`
- `core/codex.rs` helpers -> `CodexCli/Util/Codex.cs`
- `mcp-server` tool runner and config -> `CodexCli/Util/CodexToolRunner.cs` and `CodexToolCallParam.cs`
- `core/protocol.rs` events -> `CodexCli/Protocol/Event.cs`
- `exec/src` -> `CodexCli/Commands/ExecCommand.cs`

## Next Tasks
- Polish Codex spawn helpers and remaining partial implementations.
- Align provider configuration and sandbox enforcement between implementations.
- Improve API key login flow and unify Ctrl+C handling.

## Running Tests
Run unit and integration tests with:

```bash
dotnet test codex-dotnet/CodexCli.Tests/CodexCli.Tests.csproj --verbosity minimal --nologo
```

Run `dotnet restore` first if dependencies are missing.
