using CodexCli.Config;
using Xunit;

public class UriBasedFileOpenerTests
{
    [Fact]
    public void GetScheme_MatchesRustMappings()
    {
        Assert.Equal("vscode", UriBasedFileOpener.VsCode.GetScheme());
        Assert.Equal("vscode-insiders", UriBasedFileOpener.VsCodeInsiders.GetScheme());
        Assert.Equal("windsurf", UriBasedFileOpener.Windsurf.GetScheme());
        Assert.Equal("cursor", UriBasedFileOpener.Cursor.GetScheme());
        Assert.Null(UriBasedFileOpener.None.GetScheme());
    }
}
