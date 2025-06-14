using CodexCli.Util;
using CodexCli.Config;
using Xunit;

public class MarkdownUtilsTests
{
    [Fact]
    public void CitationIsRewrittenWithAbsolutePath()
    {
        var cwd = "/workspace";
        var md = "See 【F:/src/main.rs†L42-L50】 for details.";
        var res = MarkdownUtils.RewriteFileCitations(md, UriBasedFileOpener.VsCode, cwd);
        Assert.Equal("See [/src/main.rs:42](vscode://file/src/main.rs:42)  for details.", res);
    }

    [Fact]
    public void CitationIsRewrittenWithRelativePath()
    {
        var cwd = "/home/user/project";
        var md = "Refer to 【F:lib/mod.rs†L5】 here.";
        var res = MarkdownUtils.RewriteFileCitations(md, UriBasedFileOpener.Windsurf, cwd);
        Assert.Equal("Refer to [lib/mod.rs:5](windsurf://file/home/user/project/lib/mod.rs:5)  here.", res);
    }

    [Fact]
    public void CitationFollowedBySpace()
    {
        var cwd = "/home/user/project";
        var md = "References on lines 【F:src/foo.rs†L24】【F:src/foo.rs†L42】";
        var res = MarkdownUtils.RewriteFileCitations(md, UriBasedFileOpener.VsCode, cwd);
        Assert.Equal("References on lines [src/foo.rs:24](vscode://file/home/user/project/src/foo.rs:24) [src/foo.rs:42](vscode://file/home/user/project/src/foo.rs:42) ", res);
    }

    [Fact]
    public void CitationUnchangedWithoutOpener()
    {
        var cwd = "/";
        var md = "Look at 【F:file.rs†L1】.";
        var res = MarkdownUtils.RewriteFileCitations(md, UriBasedFileOpener.VsCode, cwd);
        // When opener is None, the text should stay the same via append helper, but our helper rewrites always.
        var rendered = MarkdownUtils.RewriteFileCitations(md, UriBasedFileOpener.None, cwd);
        Assert.Equal(md, rendered);
        Assert.NotEqual(md, res);
    }
}
