// Rust version at codex-rs/tui/src/text_block.rs (done)
using Spectre.Console;

namespace CodexCli.Interactive;

/// <summary>
/// Mirrors codex-rs/tui/src/text_block.rs (done)
/// </summary>
public class TextBlock : ICellWidget
{
    private readonly List<string> _lines;

    public TextBlock(IEnumerable<string> lines)
    {
        _lines = lines.ToList();
    }

    public int Height(int width)
    {
        return _lines.Sum(l => SplitLines(l, width).Count());
    }

    public IEnumerable<string> RenderWindow(int firstVisibleLine, int height, int width)
    {
        var all = new List<string>();
        foreach (var line in _lines)
            all.AddRange(SplitLines(line, width));
        return all.Skip(firstVisibleLine).Take(height);
    }

    private static List<string> SplitLines(string text, int width)
    {
        var result = new List<string>();
        for (int i = 0; i < text.Length; i += width)
            result.Add(text.Substring(i, Math.Min(width, text.Length - i)));
        if (result.Count == 0)
            result.Add(string.Empty);
        return result;
    }
}
