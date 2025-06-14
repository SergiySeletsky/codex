using System.Text;

namespace CodexCli.ApplyPatch;

public static class PatchApplier
{
    public static string Apply(string patch, string cwd)
    {
        return ApplyWithSummary(patch, cwd).Summary;
    }

    public static (AffectedPaths Affected, string Summary) ApplyWithSummary(string patch, string cwd)
    {
        List<PatchHunk> hunks;
        try
        {
            hunks = PatchParser.Parse(patch);
        }
        catch (PatchParseException e)
        {
            throw new PatchParseException($"Failed to parse patch: {e.Message}");
        }

        var added = new List<string>();
        var modified = new List<string>();
        var deleted = new List<string>();
        var stdout = new StringBuilder();
        var cwdFull = Path.GetFullPath(cwd);

        foreach (var hunk in hunks)
        {
            switch (hunk)
            {
                case AddFileHunk add:
                    var path = Path.GetFullPath(Path.Combine(cwd, add.Path));
                    if (!path.StartsWith(cwdFull))
                        throw new PatchParseException($"Path {add.Path} escapes cwd");
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, add.Contents);
                    added.Add(add.Path);
                    stdout.AppendLine($"added {add.Path}");
                    break;
                case DeleteFileHunk del:
                    var dpath = Path.GetFullPath(Path.Combine(cwd, del.Path));
                    if (!dpath.StartsWith(cwdFull))
                        throw new PatchParseException($"Path {del.Path} escapes cwd");
                    if (File.Exists(dpath))
                    {
                        File.Delete(dpath);
                        deleted.Add(del.Path);
                        stdout.AppendLine($"deleted {del.Path}");
                    }
                    break;
                case UpdateFileHunk upd:
                    var upath = Path.GetFullPath(Path.Combine(cwd, upd.Path));
                    if (!upath.StartsWith(cwdFull))
                        throw new PatchParseException($"Path {upd.Path} escapes cwd");
                    var lines = File.Exists(upath) ? File.ReadAllLines(upath).ToList() : new List<string>();
                    lines = ApplyUnifiedDiff(lines, upd.Lines);
                    if (upd.MovePath != null)
                    {
                        upath = Path.GetFullPath(Path.Combine(cwd, upd.MovePath));
                        if (!upath.StartsWith(cwdFull))
                            throw new PatchParseException($"Path {upd.MovePath} escapes cwd");
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(upath)!);
                    File.WriteAllLines(upath, lines);
                    modified.Add(upd.Path);
                    stdout.AppendLine($"updated {upd.Path}");
                    break;
            }
        }

        var summary = stdout.ToString();
        return (new AffectedPaths(added, modified, deleted), summary);
    }

    private static List<string> ApplyUnifiedDiff(List<string> original, List<string> diffLines)
    {
        var result = new List<string>();
        int index = 0;
        foreach (var line in diffLines)
        {
            if (line.StartsWith("+"))
            {
                result.Add(line.Substring(1));
            }
            else if (line.StartsWith("-"))
            {
                if (index < original.Count && original[index] == line.Substring(1))
                    index++;
            }
            else
            {
                var ctx = line.TrimStart(' ');
                if (index < original.Count && original[index] == ctx)
                {
                    result.Add(original[index]);
                    index++;
                }
            }
        }
        if (index < original.Count)
            result.AddRange(original.Skip(index));
        return result;
    }
}
