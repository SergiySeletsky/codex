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
