// Rust version at codex-rs/tui/src/cell_widget.rs (done)
namespace CodexCli.Interactive;

/// <summary>
/// Mirrors codex-rs/tui/src/cell_widget.rs (done)
/// </summary>
public interface ICellWidget
{
    int Height(int width);
    IEnumerable<string> RenderWindow(int firstVisibleLine, int height, int width);
}
