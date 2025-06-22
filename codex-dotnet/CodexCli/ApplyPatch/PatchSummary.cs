using System.IO;

namespace CodexCli.ApplyPatch;

/// <summary>
/// Ported from codex-rs/apply-patch/src/lib.rs `print_summary` (done).
/// </summary>
public static class PatchSummary
{
    public static void PrintSummary(AffectedPaths affected, TextWriter output)
    {
        output.WriteLine("Success. Updated the following files:");
        foreach (var path in affected.Added)
            output.WriteLine($"A {path}");
        foreach (var path in affected.Modified)
            output.WriteLine($"M {path}");
        foreach (var path in affected.Deleted)
            output.WriteLine($"D {path}");
    }
}
