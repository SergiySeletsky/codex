using CodexCli.Util;
using CodexCli.ApplyPatch;
using CodexCli.Commands;
using CodexCli.Protocol;
using Xunit;
using System.Collections.Generic;
using System.IO;

public class SafetyTests
{
    private static ApplyPatchAction MakeAdd(string file)
    {
        return new ApplyPatchAction(new Dictionary<string, ApplyPatchFileChange>
        {
            [file] = new ApplyPatchFileChange { Kind = "add", Content = string.Empty }
        });
    }

    [Fact]
    public void AssessPatchSafety_AutoApproveForWritableRoots()
    {
        var cwd = Directory.GetCurrentDirectory();
        var action = MakeAdd("inner.txt");
        var result = Safety.AssessPatchSafety(action, ApprovalMode.OnFailure, new List<string>{"."}, cwd);
        Assert.Equal(SafetyCheck.AutoApprove, result);
    }

    [Fact]
    public void AssessPatchSafety_AskUserUnlessAllowListed()
    {
        var cwd = Directory.GetCurrentDirectory();
        var action = MakeAdd("inner.txt");
        var result = Safety.AssessPatchSafety(action, ApprovalMode.UnlessAllowListed, new List<string>{"."}, cwd);
        Assert.Equal(SafetyCheck.AskUser, result);
    }

    [Fact]
    public void AssessCommandSafety_BasicCases()
    {
        var approved = new HashSet<List<string>>(new SequenceEqualityComparer<string>());
        var sandbox = new SandboxPolicy();
        var ok = Safety.AssessCommandSafety(new List<string>{"ls"}, ApprovalMode.OnFailure, sandbox, approved);
        Assert.Equal(SafetyCheck.AutoApprove, ok);

        var deny = Safety.AssessCommandSafety(new List<string>{"rm"}, ApprovalMode.Never, sandbox, approved);
        Assert.Equal(SafetyCheck.Reject, deny);

        var ask = Safety.AssessCommandSafety(new List<string>{"foo"}, ApprovalMode.OnFailure, sandbox, approved);
        Assert.Equal(SafetyCheck.AskUser, ask);
    }

    [Fact]
    public void AssessCommandSafety_RespectsApprovedCommands()
    {
        var state = new CodexState();
        Codex.AddApprovedCommand(state, new List<string>{"touch","foo"});
        var result = Safety.AssessCommandSafety(new List<string>{"touch","foo"}, ApprovalMode.OnFailure, new SandboxPolicy(), state.ApprovedCommands);
        Assert.Equal(SafetyCheck.AutoApprove, result);
    }
}
