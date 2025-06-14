using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodexCli.ApplyPatch;

public enum MaybeApplyPatch
{
    Body,
    ShellParseError,
    PatchParseError,
    NotApplyPatch
}

public enum MaybeApplyPatchVerified
{
    Body,
    ShellParseError,
    CorrectnessError,
    NotApplyPatch
}

public record ApplyPatchFileChange
{
    public string Kind { get; init; } = string.Empty; // "add", "delete", "update"
    public string? Content { get; init; }
    public string? UnifiedDiff { get; init; }
    public string? MovePath { get; init; }
}

public record ApplyPatchAction(Dictionary<string, ApplyPatchFileChange> Changes);

public static class ApplyPatchCommandParser
{
    public static MaybeApplyPatch MaybeParseApplyPatch(string[] argv, out string? patch)
    {
        patch = null;
        if (argv.Length >= 2 && argv[0] == "apply_patch")
        {
            patch = argv[1];
            return MaybeApplyPatch.Body;
        }
        if (argv.Length == 3 && argv[0] == "bash" && argv[1] == "-lc" && argv[2].TrimStart().StartsWith("apply_patch"))
        {
            try
            {
                patch = ExtractHeredocBodyFromApplyPatchCommand(argv[2]);
                return MaybeApplyPatch.Body;
            }
            catch
            {
                return MaybeApplyPatch.ShellParseError;
            }
        }
        return MaybeApplyPatch.NotApplyPatch;
    }

    public static MaybeApplyPatchVerified MaybeParseApplyPatchVerified(string[] argv, string cwd, out ApplyPatchAction? action)
    {
        action = null;
        if (MaybeParseApplyPatch(argv, out var patchText) != MaybeApplyPatch.Body || patchText == null)
            return MaybeApplyPatchVerified.NotApplyPatch;
        try
        {
            var hunks = PatchParser.Parse(patchText);
            var changes = new Dictionary<string, ApplyPatchFileChange>();
            foreach (var h in hunks)
            {
                switch (h)
                {
                    case AddFileHunk add:
                        changes[Path.Combine(cwd, add.Path)] = new ApplyPatchFileChange { Kind = "add", Content = add.Contents };
                        break;
                    case DeleteFileHunk del:
                        changes[Path.Combine(cwd, del.Path)] = new ApplyPatchFileChange { Kind = "delete" };
                        break;
                    case UpdateFileHunk upd:
                        changes[Path.Combine(cwd, upd.Path)] = new ApplyPatchFileChange { Kind = "update", UnifiedDiff = string.Join('\n', upd.Lines), MovePath = upd.MovePath != null ? Path.Combine(cwd, upd.MovePath) : null };
                        break;
                }
            }
            action = new ApplyPatchAction(changes);
            return MaybeApplyPatchVerified.Body;
        }
        catch (PatchParseException)
        {
            return MaybeApplyPatchVerified.CorrectnessError;
        }
    }

    private static string ExtractHeredocBodyFromApplyPatchCommand(string script)
    {
        var start = script.IndexOf("<<");
        if (start < 0) throw new InvalidOperationException();
        var newlineIndex = script.IndexOf('\n', start);
        if (newlineIndex < 0) throw new InvalidOperationException();
        var endMarker = script.Substring(start + 2, newlineIndex - (start + 2)).Trim().Trim('"', '\'', ' ');
        var endPos = script.IndexOf($"\n{endMarker}");
        if (endPos < 0) throw new InvalidOperationException();
        var bodyStart = newlineIndex + 1;
        var body = script.Substring(bodyStart, endPos - bodyStart);
        return body.TrimEnd('\n');
    }
}
