namespace CodexCli.ApplyPatch;

public record AffectedPaths
(
    List<string> Added,
    List<string> Modified,
    List<string> Deleted
);
