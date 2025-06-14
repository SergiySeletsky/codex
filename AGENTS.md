# Migration Plan: Rust Codex CLI to .NET

## Current Status
- Created new .NET console project `codex-dotnet/CodexCli` using .NET 8.
- Introduced `AppConfig` loader using Tomlyn to parse `config.toml`.
- Implemented CLI using System.CommandLine with these features:
  1. Global `--config` option to load configuration.
  2. Global `--cd` option to change working directory.
  3. Interactive mode using Spectre.Console with notification command execution.
  4. `exec` subcommand with model/profile/provider options, images, color enum,
     cwd override, git repo check and config overrides.
  5. `login` subcommand saves token to `~/.codex/token` and reads `CODEX_TOKEN`
     environment variable. Supports config overrides.
  6. `mcp` subcommand starts a basic HTTP listener.
  7. `proto` subcommand that streams stdin lines.
  8. `debug seatbelt` placeholder.
  9. `debug landlock` placeholder.
 10. Interactive mode stores history with `/history`, `/help`, and `/quit`.
 11. Proper subcommand routing with async handlers.
 12. Added simple unit tests for config overrides parsing.
- Build verified with `dotnet build`.
- Tests run with `dotnet test`.

## Files Migrated
- `codex-dotnet/CodexCli` project with Program.cs, command implementations under `Commands/`, and `Config/AppConfig.cs`.

## TODO Next Run
- Flesh out exec logic and integrate with agent core library.
- Expand MCP server beyond basic listener and implement protocol event streaming.
- Port remaining Rust modules (session management, agent core) to .NET libraries.
- Enhance interactive TUI with additional commands and state persistence.
- Add more unit tests for config loading and command parsing.
