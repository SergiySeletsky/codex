using Spectre.Console;
using CodexCli.Protocol;
using CodexCli.Util;

namespace CodexCli.Interactive;

/// <summary>
/// Simple user approval prompt used in the TUI.
/// Mirrors codex-rs/tui/src/user_approval_widget.rs (done).
/// </summary>
public class UserApprovalWidget
{
    private readonly Func<string?> _readLine;

    public UserApprovalWidget(Func<string?>? readLine = null)
    {
        _readLine = readLine ?? Console.ReadLine;
    }

    public ReviewDecision PromptExec(string[] command, string cwd, string? reason = null)
    {
        var cmd = ExecCommandUtils.StripBashLcAndEscape(command);
        var cwdDisp = ExecCommandUtils.RelativizeToHome(cwd);
        cwdDisp = cwdDisp != null ? $"~/{cwdDisp}" : cwd;

        AnsiConsole.MarkupLine("[bold]Shell Command[/]");
        AnsiConsole.MarkupLine($"[dim]{cwdDisp}$[/] {cmd}");
        if (!string.IsNullOrEmpty(reason))
            AnsiConsole.MarkupLine(reason);
        AnsiConsole.Markup(Markup.Escape("Allow command? [y/a/n/q] "));
        var input = _readLine()?.Trim().ToLowerInvariant();
        return input switch
        {
            "y" or "yes" => ReviewDecision.Approved,
            "a" => ReviewDecision.ApprovedForSession,
            "q" or "abort" => ReviewDecision.Abort,
            _ => ReviewDecision.Denied
        };
    }

    public ReviewDecision PromptPatch(string summary)
    {
        AnsiConsole.MarkupLine("[bold]Apply patch[/]");
        AnsiConsole.MarkupLine(summary);
        AnsiConsole.Markup(Markup.Escape("Allow changes? [y/n/q] "));
        var input = _readLine()?.Trim().ToLowerInvariant();
        return input switch
        {
            "y" or "yes" => ReviewDecision.Approved,
            "q" or "abort" => ReviewDecision.Abort,
            _ => ReviewDecision.Denied
        };
    }
}
