# Migration Plan: Rust Codex CLI to .NET

## Current Status
- Created new .NET console project `codex-dotnet/CodexCli` using .NET 8.
- Introduced `AppConfig` loader using Tomlyn to parse `config.toml`.
- Implemented CLI using System.CommandLine with these features:
  1. Global `--config` option to load configuration.
  2. Global `--cd` option to change working directory.
  3. Interactive mode using Spectre.Console with notification command execution.
  4. `exec` subcommand with model/profile options, images, color settings,
     git repo check and config overrides.
  5. `login` subcommand saves token to `~/.codex/token`.
  6. `mcp` subcommand placeholder.
  7. `proto` subcommand that streams stdin lines.
  8. `debug seatbelt` placeholder.
  9. `debug landlock` placeholder.
 10. Interactive mode stores history with `/history` and `/quit` commands.
 11. Proper subcommand routing with async handlers.
- Build verified with `dotnet build`.

## Files Migrated
- `codex-dotnet/CodexCli` project with Program.cs, command implementations under `Commands/`, and `Config/AppConfig.cs`.

## TODO Next Run
- Flesh out exec logic and integrate with agent core library.
- Implement MCP server functionality and protocol event streaming.
- Port remaining Rust modules (session management, agent core) to .NET libraries.
- Expand interactive TUI with richer slash commands.
- Add automated tests for config loading and exec parsing.
