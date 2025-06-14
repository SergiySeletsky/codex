using CodexCli.Commands;
namespace CodexCli.Config;

public class ConfigProfile
{
    public string? Model { get; set; }
    public string? ModelProvider { get; set; }
    public ApprovalMode? ApprovalPolicy { get; set; }
    public bool? DisableResponseStorage { get; set; }
}
