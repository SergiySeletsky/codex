using CodexCli.Commands;
namespace CodexCli.Config;

public class ConfigProfile
{
    public string? Model { get; set; }
    public string? ModelProvider { get; set; }
    public ApprovalMode? ApprovalPolicy { get; set; }
    public bool? DisableResponseStorage { get; set; }
    public ReasoningEffort? ModelReasoningEffort { get; set; }
    public ReasoningSummary? ModelReasoningSummary { get; set; }
}
