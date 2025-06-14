using System.IO;
using System.Text;

namespace CodexCli.ApplyPatch;

public static class PatchApplier
{
    public static string Apply(string patch, string cwd)
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
                    stdout.AppendLine($"added {add.Path}");
                    break;
                case DeleteFileHunk del:
                    var dpath = Path.GetFullPath(Path.Combine(cwd, del.Path));
                    if (!dpath.StartsWith(cwdFull))
                        throw new PatchParseException($"Path {del.Path} escapes cwd");
                    if (File.Exists(dpath))
                    {
                        File.Delete(dpath);
                        stdout.AppendLine($"deleted {del.Path}");
                    }
                    break;
                case UpdateFileHunk upd:
                    var upath = Path.GetFullPath(Path.Combine(cwd, upd.Path));
                    if (!upath.StartsWith(cwdFull))
                        throw new PatchParseException($"Path {upd.Path} escapes cwd");
                    var lines = File.Exists(upath) ? File.ReadAllLines(upath).ToList() : new List<string>();
                    int idx = 0;
                    foreach (var line in upd.Lines)
                    {
                        if (line.StartsWith("+"))
                        {
                            lines.Insert(idx, line[1..]);
                            idx++;
                        }
                        else if (line.StartsWith("-"))
                        {
                            if (idx < lines.Count && lines[idx] == line[1..])
                                lines.RemoveAt(idx);
                        }
                        else
                        {
                            if (idx < lines.Count && lines[idx] == line.TrimStart(' '))
                                idx++;
                        }
                    }
                    if (upd.MovePath != null)
                    {
                        upath = Path.GetFullPath(Path.Combine(cwd, upd.MovePath));
                        if (!upath.StartsWith(cwdFull))
                            throw new PatchParseException($"Path {upd.MovePath} escapes cwd");
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(upath)!);
                    File.WriteAllLines(upath, lines);
                    stdout.AppendLine($"updated {upd.Path}");
                    break;
            }
        }
        return stdout.ToString();
    }
}
