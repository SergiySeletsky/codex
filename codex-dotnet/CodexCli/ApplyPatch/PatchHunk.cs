namespace CodexCli.ApplyPatch;

public abstract record PatchHunk;

public record AddFileHunk(string Path, string Contents) : PatchHunk;
public record DeleteFileHunk(string Path) : PatchHunk;
public record UpdateFileHunk(string Path, string? MovePath, List<string> Lines) : PatchHunk;
