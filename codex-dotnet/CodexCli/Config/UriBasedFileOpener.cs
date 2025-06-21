/// <summary>
/// Port of codex-rs/core/src/config_types.rs UriBasedFileOpener (done).
/// </summary>
namespace CodexCli.Config;

public enum UriBasedFileOpener
{
    VsCode,
    VsCodeInsiders,
    Windsurf,
    Cursor,
    None,
}

public static class UriBasedFileOpenerExtensions
{
    public static string? GetScheme(this UriBasedFileOpener opener) => opener switch
    {
        UriBasedFileOpener.VsCode => "vscode",
        UriBasedFileOpener.VsCodeInsiders => "vscode-insiders",
        UriBasedFileOpener.Windsurf => "windsurf",
        UriBasedFileOpener.Cursor => "cursor",
        _ => null,
    };
}
