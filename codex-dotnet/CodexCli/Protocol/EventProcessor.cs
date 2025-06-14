using CodexCli.Util;
using Spectre.Console;

namespace CodexCli.Protocol;

public class EventProcessor
{
    private readonly Style _bold;
    private readonly Style _cyan;
    private readonly Style _dim;
    private readonly Style _red;
    private readonly Style _green;

    public EventProcessor(bool withAnsi)
    {
        _bold = withAnsi ? new Style(decoration: Decoration.Bold) : Style.Plain;
        _cyan = withAnsi ? new Style(foreground: Color.CadetBlue) : Style.Plain;
        _dim = withAnsi ? new Style(decoration: Decoration.Dim) : Style.Plain;
        _red = withAnsi ? new Style(foreground: Color.Red) : Style.Plain;
        _green = withAnsi ? new Style(foreground: Color.Green) : Style.Plain;
    }

    public void PrintConfigSummary(string model, string cwd, string prompt)
    {
        AnsiConsole.MarkupLine($"[grey]{Elapsed.Timestamp()}[/] [bold]model:[/] {model}");
        AnsiConsole.MarkupLine($"[grey]{Elapsed.Timestamp()}[/] [bold]cwd:[/] {cwd}");
        AnsiConsole.MarkupLine("--------");
        AnsiConsole.MarkupLine($"[grey]{Elapsed.Timestamp()}[/] [bold cyan]User instructions:[/]\n{prompt}");
    }

    public void ProcessEvent(Event ev)
    {
        string ts = $"[grey]{Elapsed.Timestamp()}[/]";
        switch (ev)
        {
            case AgentMessageEvent msg:
                AnsiConsole.MarkupLine($"{ts} [bold magenta]codex[/]\n{msg.Message}");
                break;
            case BackgroundEvent bg:
                AnsiConsole.MarkupLine($"{ts} [dim]{bg.Message}[/]");
                break;
            case ErrorEvent err:
                AnsiConsole.MarkupLine($"{ts} [red]ERROR:[/] {err.Message}");
                break;
            case ExecCommandBeginEvent begin:
                var cmd = string.Join(' ', begin.Command.Select(p => Markup.Escape(p)));
                AnsiConsole.MarkupLine($"{ts} [magenta]exec[/] [bold]{cmd}[/] in {begin.Cwd}");
                break;
            case ExecCommandEndEvent end:
                var style = end.ExitCode == 0 ? _green : _red;
                var title = end.ExitCode == 0 ? "succeeded" : $"exited {end.ExitCode}";
                AnsiConsole.MarkupLine($"{ts} [magenta]exec[/] [bold]{title}[/]");
                var output = (end.ExitCode == 0 ? end.Stdout : end.Stderr).Split('\n');
                foreach (var line in output.Take(20))
                    AnsiConsole.MarkupLine($"[dim]{Markup.Escape(line)}[/]");
                break;
            case ExecApprovalRequestEvent ar:
                var cmd2 = string.Join(' ', ar.Command.Select(Markup.Escape));
                AnsiConsole.MarkupLine($"{ts} [yellow]approval required for[/] [bold]{cmd2}[/]");
                break;
            case PatchApplyApprovalRequestEvent pr:
                AnsiConsole.MarkupLine($"{ts} [yellow]patch approval required:[/] {Markup.Escape(pr.PatchSummary)}");
                break;
            case TaskCompleteEvent tc:
                AnsiConsole.MarkupLine($"{ts} task complete");
                if (tc.LastAgentMessage != null)
                    AnsiConsole.MarkupLine($"{tc.LastAgentMessage}");
                break;
        }
    }
}
