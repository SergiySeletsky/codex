// Port of codex-rs/core/src/safety.rs assess_patch_safety and helpers (done)
using CodexCli.ApplyPatch;
using CodexCli.Protocol;
using CodexCli.Models;
using CodexCli.Commands;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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

        if (IsWritePatchConstrainedToWritableRoots(action, writableRoots, cwd))
            return SafetyCheck.AutoApprove;

        return policy switch
        {
            ApprovalMode.OnFailure => SafetyCheck.AutoApprove,
            ApprovalMode.Never => SafetyCheck.Reject,
            _ => SafetyCheck.AskUser
        };
    }

    internal static bool IsWritePatchConstrainedToWritableRoots(ApplyPatchAction action, List<string> writableRoots, string cwd)
    {
        if (writableRoots.Count == 0)
            return false;

        bool IsWritable(string path)
        {
            var abs = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(cwd, path));
            return writableRoots.Any(root =>
            {
                var rootAbs = Path.GetFullPath(Path.IsPathRooted(root) ? root : Path.Combine(cwd, root));
                return abs.StartsWith(rootAbs);
            });
        }

        foreach (var kv in action.Changes)
        {
            var path = kv.Key;
            var change = kv.Value;
            switch (change.Kind)
            {
                case "add":
                case "delete":
                    if (!IsWritable(path))
                        return false;
                    break;
                case "update":
                    if (!IsWritable(path))
                        return false;
                    if (change.MovePath != null && !IsWritable(change.MovePath))
                        return false;
                    break;
            }
        }

        return true;
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

    // Helpers for earlier versions retained for completeness, though not used
    // in current ports.
}
