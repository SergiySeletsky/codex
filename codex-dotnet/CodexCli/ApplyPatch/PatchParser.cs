using System.Text;
using System.Text.RegularExpressions;

namespace CodexCli.ApplyPatch;

public static class PatchParser
{
    private static readonly Regex AddFileRegex = new(@"^\*\*\* Add File: (?<path>.+)$");
    private static readonly Regex DeleteFileRegex = new(@"^\*\*\* Delete File: (?<path>.+)$");
    private static readonly Regex UpdateFileRegex = new(@"^\*\*\* Update File: (?<path>.+)$");
    private static readonly Regex MoveToRegex = new(@"^\*\*\* Move to: (?<path>.+)$");

    public static List<PatchHunk> Parse(string patch)
    {
        var lines = patch.Trim().Split('\n');
        if (lines.Length < 2 || lines[0].Trim() != "*** Begin Patch" || lines[^1].Trim() != "*** End Patch")
            throw new PatchParseException("patch missing begin/end markers");
        var hunks = new List<PatchHunk>();
        int i = 1;
        while (i < lines.Length - 1)
        {
            var line = lines[i].TrimEnd();
            var m1 = AddFileRegex.Match(line);
            if (m1.Success)
            {
                i++;
                var sb = new StringBuilder();
                while (i < lines.Length - 1 && !lines[i].StartsWith("***"))
                {
                    var l = lines[i];
                    if (l.StartsWith('+')) sb.AppendLine(l[1..]);
                    i++;
                }
                hunks.Add(new AddFileHunk(m1.Groups["path"].Value, sb.ToString()));
                continue;
            }
            var m2 = DeleteFileRegex.Match(line);
            if (m2.Success)
            {
                i++;
                hunks.Add(new DeleteFileHunk(m2.Groups["path"].Value));
                continue;
            }
            var m3 = UpdateFileRegex.Match(line);
            if (m3.Success)
            {
                i++;
                string? movePath = null;
                var mm = (i < lines.Length - 1) ? MoveToRegex.Match(lines[i].Trim()) : null;
                if (mm != null && mm.Success)
                {
                    movePath = mm.Groups["path"].Value;
                    i++;
                }
                var updateLines = new List<string>();
                while (i < lines.Length - 1 && !lines[i].StartsWith("***"))
                {
                    if (lines[i].Trim() == "*** End of File")
                    {
                        updateLines.Add(lines[i].Trim());
                        i++;
                        break;
                    }
                    updateLines.Add(lines[i]);
                    i++;
                }
                hunks.Add(new UpdateFileHunk(m3.Groups["path"].Value, movePath, updateLines));
                continue;
            }
            i++;
        }
        return hunks;
    }

    public static List<string> ParseUnified(string diff)
    {
        var lines = diff.Trim().Split('\n');
        var data = new List<string>();
        foreach (var line in lines)
        {
            if (line.StartsWith("---") || line.StartsWith("+++"))
                continue;
            if (line.StartsWith("@@"))
                continue;
            data.Add(line);
        }
        return data;
    }
}
