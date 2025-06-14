# Migration Plan: Rust Codex CLI to .NET

## Current Status
- Created new .NET console project `codex-dotnet/CodexCli` using .NET 8.
- Introduced `AppConfig` loader using Tomlyn to parse `config.toml`.
- Implemented CLI using System.CommandLine with these features:
  1. Global `--config` option to load configuration.
  2. Global `--cd` option to change working directory.
  3. Interactive mode using Spectre.Console with notification command execution.
  4. `exec` subcommand accepting a prompt.
  5. `login` subcommand placeholder.
  6. `mcp` subcommand placeholder.
  7. `proto` subcommand that streams stdin lines.
  8. `debug seatbelt` placeholder.
  9. `debug landlock` placeholder.
  10. Proper subcommand routing with async handlers.
- Build verified with `dotnet build`.

## Files Migrated
- `codex-dotnet/CodexCli` project with Program.cs, command implementations under `Commands/`, and `Config/AppConfig.cs`.

## TODO Next Run
- Flesh out exec logic and integrate with agent core.
- Implement login token management.
- Add MCP server functionality and protocol event streaming.
- Port remaining Rust modules (session management, agent core) to .NET libraries.
- Expand interactive TUI with conversation history and slash commands.
- Add automated tests.
