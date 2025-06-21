// Port of codex-rs/core/src/safety.rs (simplified, done)
using CodexCli.ApplyPatch;
using CodexCli.Protocol;
using CodexCli.Models;
using CodexCli.Commands;

namespace CodexCli.Util;

public enum SafetyCheck
{
    AutoApprove,
    AskUser,
    Reject
}

public static class Safety
{
    public static SafetyCheck AssessPatchSafety(ApplyPatchAction action, ApprovalMode policy, List<string> writableRoots, string cwd)
    {
        if (action.Changes.Count == 0)
            return SafetyCheck.Reject;

        if (policy == ApprovalMode.UnlessAllowListed)
            return SafetyCheck.AskUser;

        bool allWritable = action.Changes.Keys.All(p => IsPathWritable(Path.Combine(cwd, p), writableRoots));

        if (allWritable)
            return SafetyCheck.AutoApprove;

        return policy switch
        {
            ApprovalMode.OnFailure => SafetyCheck.AutoApprove,
            ApprovalMode.Never => SafetyCheck.Reject,
            _ => SafetyCheck.AskUser
        };
    }

    public static SafetyCheck AssessCommandSafety(List<string> command, ApprovalMode policy, SandboxPolicy sandbox, HashSet<List<string>> approved)
    {
        if (SafeCommand.IsKnownSafeCommand(command) || approved.Contains(command))
            return SafetyCheck.AutoApprove;

        if (sandbox.IsUnrestricted())
            return SafetyCheck.AutoApprove;

        if (policy == ApprovalMode.Never)
            return SafetyCheck.Reject;

        return SafetyCheck.AskUser;
    }

    private static bool IsPathWritable(string path, List<string> roots)
        => roots.Count == 0 || roots.Any(r => Path.GetFullPath(path).StartsWith(Path.GetFullPath(r)));
}
