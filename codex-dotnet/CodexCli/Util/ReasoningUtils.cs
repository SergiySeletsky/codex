using CodexCli.Commands;
using CodexCli.Models;

namespace CodexCli.Util;

public static class ReasoningUtils
{
    public static Reasoning? CreateReasoningParam(string model, ReasoningEffort effort, ReasoningSummary summary)
    {
        var eff = effort switch
        {
            ReasoningEffort.Low => OpenAiReasoningEffort.Low,
            ReasoningEffort.Medium => OpenAiReasoningEffort.Medium,
            ReasoningEffort.High => OpenAiReasoningEffort.High,
            _ => (OpenAiReasoningEffort?)null
        };

        if (!eff.HasValue)
            return null;

        if (ModelSupportsReasoningSummaries(model))
        {
            OpenAiReasoningSummary? sum = summary switch
            {
                ReasoningSummary.Brief => OpenAiReasoningSummary.Concise,
                ReasoningSummary.Detailed => OpenAiReasoningSummary.Detailed,
                _ => OpenAiReasoningSummary.Auto
            };
            return new Reasoning(eff.Value, sum);
        }
        return null;
    }

    public static bool ModelSupportsReasoningSummaries(string model)
        => model.StartsWith("o") || model.StartsWith("codex");
}
