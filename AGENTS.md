# Migration Plan: Rust Codex CLI to .NET

## Completed Summary
- Bootstrapped the .NET CLI with configuration loading, interactive mode, history management, sandbox enforcement and patch replay.
- Ported provider management, API key handling and TUI widgets with approval workflow.
- Added MCP client/server support with SSE events and cross-language tests.
- Migrated utilities such as ExecCommandUtils, SafeCommand, ExecRunner/ExecEnv, OpenAiApiKey, OpenAiTools, Backoff, GitUtils and SignalUtils.
- Ported project documentation, user notifications, environment flags, model provider registry, config profile helpers, configuration types, conversation and message history, approval mode parsing, config overrides, elapsed time helpers and MCP tool call.
- Updated Rust and C# sources with status comments and added parity tests for each port.
- Implemented ChatCompletions aggregator and hooked it into ModelClient with new unit and integration tests.
- Fixed recursion bug in ResponseStream.Aggregate and verified aggregation tests pass.
- Ported Codex error enums and EnvVarError helper with new unit tests.

## Rust to C# Mapping
- codex-rs/tui/src/exec_command.rs -> codex-dotnet/CodexCli/Util/ExecCommandUtils.cs (done)
- codex-rs/core/src/is_safe_command.rs -> codex-dotnet/CodexCli/Util/SafeCommand.cs (done)
- codex-rs/core/src/exec.rs -> codex-dotnet/CodexCli/Util/ExecRunner.cs (done)
- codex-rs/core/src/exec_env.rs -> codex-dotnet/CodexCli/Util/ExecEnv.cs (done)
- codex-rs/core/src/openai_api_key.rs -> codex-dotnet/CodexCli/Util/OpenAiApiKey.cs (done)
- codex-rs/core/src/openai_tools.rs -> codex-dotnet/CodexCli/Util/OpenAiTools.cs (done)
- codex-rs/core/src/util.rs -> codex-dotnet/CodexCli/Util/{Backoff.cs,GitUtils.cs,SignalUtils.cs} (done)
- codex-rs/core/src/project_doc.rs -> codex-dotnet/CodexCli/Util/ProjectDoc.cs (done)
- codex-rs/core/src/user_notification.rs -> codex-dotnet/CodexCli/Util/UserNotification.cs (done)
- codex-rs/core/src/flags.rs -> codex-dotnet/CodexCli/Config/EnvFlags.cs (done)
- codex-rs/core/src/model_provider_info.rs -> codex-dotnet/CodexCli/Config/ModelProviderInfo.cs (done)
- codex-rs/core/src/config_profile.rs -> codex-dotnet/CodexCli/Config/ConfigProfile.cs (done)
- codex-rs/core/src/config_types.rs -> codex-dotnet/CodexCli/Config/{History.cs,ShellEnvironmentPolicy.cs,Tui.cs,UriBasedFileOpener.cs,ReasoningModels.cs} (done)
- codex-rs/core/src/client_common.rs -> codex-dotnet/CodexCli/{Models/{Prompt.cs,ResponseEvent.cs,ReasoningModels.cs},Util/{ReasoningUtils.cs,ModelClient.cs}} (done)
- codex-rs/core/src/conversation_history.rs -> codex-dotnet/CodexCli/Util/ConversationHistory.cs (done)
- codex-rs/core/src/message_history.rs -> codex-dotnet/CodexCli/Util/MessageHistory.cs (done)
- codex-rs/common/src/approval_mode_cli_arg.rs -> codex-dotnet/CodexCli/Commands/ApprovalModeCliArg.cs (done)
- codex-rs/common/src/config_override.rs -> codex-dotnet/CodexCli/Config/ConfigOverrides.cs (done)
- codex-rs/common/src/elapsed.rs -> codex-dotnet/CodexCli/Util/Elapsed.cs (done)
- codex-rs/core/src/mcp_tool_call.rs -> codex-dotnet/CodexCli/Util/McpToolCall.cs (done)
- codex-rs/core/src/chat_completions.rs -> codex-dotnet/CodexCli/Util/ChatCompletions.cs (done)
- codex-rs/core/src/error.rs -> codex-dotnet/CodexCli/Util/CodexErr.cs (done)

## TODO
- Integrate newly ported utilities throughout CLI commands and finalize SSE handling.
- Expand CLI and cross-language parity tests and fix flakes, including chat aggregation.
- Add sandbox enforcement logic and wire ApprovalModeCliArg and ExecEnv into command execution.
- Improve API key login flow using OpenAiApiKey helper and implement Ctrl+C handling via SignalUtils.
- Implement CLI comparitive tests ensuring .NET and Rust outputs match for chat aggregation.
- Finish port of session initialization via CodexWrapper.
