using CodexCli.Config;
using CodexCli.Interactive;
using CodexCli.Protocol;
using Spectre.Console;

namespace CodexTui;

/// <summary>
/// Initial prototype TUI host wiring the BottomPane for user input.
/// Mirrors codex-rs/tui/src/app.rs (very partially, in progress).
/// </summary>
internal static class TuiApp
{
    public static Task<int> RunAsync(InteractiveOptions opts, AppConfig? cfg)
    {
        var sender = new AppEventSender(_ => { });
        var pane = new BottomPane(sender, hasInputFocus: true);
        var chat = new ChatWidget();
        using var status = new StatusIndicatorWidget();
        status.Start();
        chat.AddAgentMessage("Codex TUI prototype - type /quit to exit");
        if (!string.IsNullOrEmpty(opts.Prompt))
            chat.AddUserMessage(opts.Prompt);
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            var res = pane.HandleKeyEvent(key);
            if (res.IsSubmitted)
            {
                var text = res.SubmittedText!;
                chat.AddUserMessage(text);
                if (text.Equals("/quit", StringComparison.OrdinalIgnoreCase))
                    break;
                // TODO: send text to agent and display responses
                chat.AddAgentMessage($"echo: {text}");
            }
        }
        return Task.FromResult(0);
    }
}
