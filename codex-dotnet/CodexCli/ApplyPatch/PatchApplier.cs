using System.IO;
using System.Text;

namespace CodexCli.ApplyPatch;

public static class PatchApplier
{
    public static string Apply(string patch, string cwd)
    {
        var hunks = PatchParser.Parse(patch);
        var stdout = new StringBuilder();
        foreach (var hunk in hunks)
        {
            switch (hunk)
            {
                case AddFileHunk add:
                    var path = Path.Combine(cwd, add.Path);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    File.WriteAllText(path, add.Contents);
                    stdout.AppendLine($"added {add.Path}");
                    break;
                case DeleteFileHunk del:
                    var dpath = Path.Combine(cwd, del.Path);
                    if (File.Exists(dpath))
                    {
                        File.Delete(dpath);
                        stdout.AppendLine($"deleted {del.Path}");
                    }
                    break;
                case UpdateFileHunk upd:
                    var upath = Path.Combine(cwd, upd.Path);
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
                        upath = Path.Combine(cwd, upd.MovePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(upath)!);
                    File.WriteAllLines(upath, lines);
                    stdout.AppendLine($"updated {upd.Path}");
                    break;
            }
        }
        return stdout.ToString();
    }
}
