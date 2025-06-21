/// <summary>
/// Port of codex-rs/core/src/client_common.rs reasoning enums (done).
/// </summary>
namespace CodexCli.Models;

using System.Text.Json.Serialization;

public enum OpenAiReasoningEffort
{
    Low,
    Medium,
    High
}

public enum OpenAiReasoningSummary
{
    Auto,
    Concise,
    Detailed
}

public record Reasoning(OpenAiReasoningEffort Effort, OpenAiReasoningSummary? Summary);
