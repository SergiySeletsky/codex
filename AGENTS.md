# Migration Plan: Rust Codex CLI to .NET

## Current Status
- Created new .NET console project `codex-dotnet/CodexCli` using .NET 8.
- Introduced `AppConfig` loader using Tomlyn to parse `config.toml`.
- Implemented manual CLI parsing with following features:
  1. Global `--config` option to load configuration.
  2. Global `--cd` option to change working directory.
  3. Default interactive mode with optional notification command execution.
  4. `exec` subcommand accepting a prompt.
  5. `login` subcommand placeholder.
  6. `mcp` subcommand placeholder.
  7. `proto` subcommand placeholder.
  8. `debug seatbelt` placeholder.
  9. `debug landlock` placeholder.
- Build verified with `dotnet build`.

## Files Migrated
- `codex-dotnet/CodexCli` project added with Program.cs and `Config/AppConfig.cs`.

## TODO Next Run
- Expand interactive mode with TUI library (e.g., Spectre.Console).
- Implement real exec logic and integrate with agent core.
- Flesh out login flow and token management.
- Add MCP server functionality and protocol streaming.
- Begin porting core Rust logic (session management, event handling) into .NET libraries.
- Add automated tests.
