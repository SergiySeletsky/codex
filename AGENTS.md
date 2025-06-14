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
 13. Added Git repo root detection utility.
 14. Implemented elapsed time formatter and tests.
 15. Expanded config loader to support notify arrays and CODEX_HOME env var.
 16. Added approval and sandbox options for exec and interactive commands.
 17. Interactive command now accepts prompt, model and sandbox options.
 18. Added SandboxPermission parser with disk-write-folder support.
 19. Added OpenAI API key manager and extended login command to set it.
 20. Interactive command reads initial prompt from stdin when '-' is used.
21. Added utilities for CODEX_HOME and log directory detection.
22. Added tests for sandbox permission parsing and git repo detection.
23. Introduced simplified protocol events and EventProcessor for printing agent output.
24. Added MockCodexAgent to simulate event streams for exec command.
25. Exec command now prints config summary and processes events, writing last message to file.
26. Interactive mode runs notify command on start and completion and adds /log command.
27. Added NotifyUtils helper and ExitStatus propagation for debug commands.
28. Implemented basic seatbelt/landlock debug commands executing processes.
29. Proto command parses JSON-RPC messages and prints method names.
30. Added timestamp helper in Elapsed and utilities tests.
31. Added EnvUtilsTests verifying CODEX_HOME environment variable handling.
32. Added event and protocol classes under `Protocol/`.
33. Added session manager, /config and /save commands in interactive mode.
34. EnvUtils now supports CODEX_HISTORY_DIR and history directory lookup with tests.
35. Implemented new approval request events and handling in ExecCommand.
36. MockCodexAgent emits approval events; ExecCommand prompts user.
37. Integrated basic OpenAIClient stub in ExecCommand.
38. Added SessionManager tests and OpenAIClient placeholder.
39. Added PatchApply, McpToolCall, AgentReasoning and SessionConfigured events.
40. Extended EventProcessor to display these events.
41. MockCodexAgent now emits diverse events for testing.
42. Interactive command supports provider, color, notify and override options plus /reset command.
43. SessionManager exposes ClearHistory and associated tests.
44. Added EnvUtilsTests for default history dir.
45. SessionManager now writes history entries to files under `GetHistoryDir`.
46. Exec command records agent messages and reports the history file path.
47. Interactive command prints the history file path when exiting.
48. Added `model_provider` support in `AppConfig` and displayed in config summary.
49. Exec command now honors provider from config or CLI.
50. Implemented basic SSE streaming in `mcp` command on `/events`.
51. Added binder unit tests for `ExecBinder` and `InteractiveBinder`.
52. SessionManager tests verify history file creation.
- Build verified with `dotnet build`.
- Tests run with `dotnet test`.

## Files Migrated
- `codex-dotnet/CodexCli` project with Program.cs, command implementations under `Commands/`, and `Config/AppConfig.cs`.

## TODO Next Run
- Integrate exec command with real Codex agent and tool execution.
- Expand MCP server to handle multiple connections and serve real events.
- Add more comprehensive unit tests for binder logic.
- Continue porting Rust protocol types and core libraries.
