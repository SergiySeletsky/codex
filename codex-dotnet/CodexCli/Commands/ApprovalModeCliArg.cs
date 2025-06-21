// Ported from codex-rs/common/src/approval_mode_cli_arg.rs (done)
using System.Linq;
using CodexCli.Protocol;

namespace CodexCli.Commands;

public enum ApprovalModeCliArg
{
    OnFailure,
    UnlessAllowListed,
    Never,
}

public static class ApprovalModeCliArgExtensions
{
    public static ApprovalMode ToApprovalMode(this ApprovalModeCliArg arg) => arg switch
    {
        ApprovalModeCliArg.OnFailure => ApprovalMode.OnFailure,
        ApprovalModeCliArg.UnlessAllowListed => ApprovalMode.UnlessAllowListed,
        ApprovalModeCliArg.Never => ApprovalMode.Never,
        _ => ApprovalMode.OnFailure,
    };
}

public static class SandboxPermissionOption
{
    public static SandboxPermission[] Parse(string[] raw, string basePath)
    {
        return raw.Select(r => SandboxPermissionParser.Parse(r, basePath)).ToArray();
    }
}
