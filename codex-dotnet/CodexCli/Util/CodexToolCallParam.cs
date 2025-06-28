// Ported from codex-rs/mcp-server/src/codex_tool_config.rs (partial, schema generation omitted)
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodexCli.Util;

public record CodexToolCallParam(
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("model")] string? Model = null,
    [property: JsonPropertyName("profile")] string? Profile = null,
    [property: JsonPropertyName("cwd")] string? Cwd = null,
    [property: JsonPropertyName("approval-policy")] string? ApprovalPolicy = null,
    [property: JsonPropertyName("sandbox-permissions")] IReadOnlyList<string>? SandboxPermissions = null,
    [property: JsonPropertyName("config")] Dictionary<string, JsonElement>? Config = null,
    [property: JsonPropertyName("provider")] string? Provider = null);

