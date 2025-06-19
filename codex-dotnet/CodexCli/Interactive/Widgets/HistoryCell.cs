using System.Collections.Generic;

namespace CodexCli.Interactive;

/// <summary>
/// Mirrors codex-rs/tui/src/history_cell.rs (basic image dimension rendering done)
/// </summary>
internal class HistoryCell : ICellWidget
{
    internal enum CellType
    {
        Generic,
        Welcome,
        SessionInfo,
        User,
        Agent,
        System,
        Reasoning,
        Background,
        Error,
        ExecCommand,
        ExecResult,
        PatchBegin,
        PatchEnd,
        PendingPatch,
        ToolBegin,
        ToolEnd,
        ToolImage,
        HistoryEntry
    }

    internal readonly CellType Type;
    private readonly TextBlock _block;

    internal HistoryCell(CellType type, IEnumerable<string> lines)
    {
        Type = type;
        _block = new TextBlock(lines);
    }

    public int Height(int width) => _block.Height(width);

    public IEnumerable<string> RenderWindow(int firstVisibleLine, int height, int width) =>
        _block.RenderWindow(firstVisibleLine, height, width);
}
