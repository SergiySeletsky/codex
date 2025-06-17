using CodexCli.Config;
using CodexCli.Commands;
using CodexCli.Interactive;
using CodexCli.Protocol;
using CodexCli.Util;
using Spectre.Console;

namespace CodexTui;

/// <summary>
/// Initial prototype TUI host wiring the BottomPane for user input.
/// Mirrors codex-rs/tui/src/app.rs (very partially, in progress).
/// </summary>
internal static class TuiApp
{
    public static async Task<int> RunAsync(InteractiveOptions opts, AppConfig? cfg)
    {
        var sender = new AppEventSender(_ => { });
        var pane = new BottomPane(sender, hasInputFocus: true);
        var chat = new ChatWidget();
        using var status = new StatusIndicatorWidget();
        status.Start();
        chat.AddAgentMessage("Codex TUI prototype - type /quit to exit");
        if (!string.IsNullOrEmpty(opts.Prompt))
            chat.AddUserMessage(opts.Prompt);

        string providerId = opts.ModelProvider ?? cfg?.ModelProvider ?? "Mock";
        bool hideReason = opts.HideAgentReasoning ?? cfg?.HideAgentReasoning ?? false;
        var processor = new EventProcessor(withAnsi: !Console.IsOutputRedirected,
            showReasoning: !hideReason,
            UriBasedFileOpener.None,
            Environment.CurrentDirectory);

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

                status.UpdateText("thinking...");

                var events = providerId == "Mock"
                    ? MockCodexAgent.RunAsync(text, Array.Empty<string>(), InteractiveApp.ApprovalHandler)
                    : RealCodexAgent.RunAsync(text,
                        new OpenAIClient(ApiKeyManager.GetKey(ModelProviderInfo.BuiltIns[providerId]),
                            ModelProviderInfo.BuiltIns[providerId].BaseUrl),
                        opts.Model ?? cfg?.Model ?? "default",
                        InteractiveApp.ApprovalHandler ?? (_ => Task.FromResult(ReviewDecision.Approved)));

                await foreach (var ev in events)
                {
                    processor.ProcessEvent(ev);
                    switch (ev)
                    {
                        case AgentMessageEvent am:
                            chat.AddAgentMessage(am.Message);
                            break;
                        case BackgroundEvent bg:
                            chat.AddSystemMessage(bg.Message);
                            break;
                        case ErrorEvent err:
                            chat.AddSystemMessage($"ERROR: {err.Message}");
                            break;
                        case TaskCompleteEvent tc:
                            if (tc.LastAgentMessage != null)
                                chat.AddAgentMessage(tc.LastAgentMessage);
                            status.UpdateText("ready");
                            break;
                    }
                }
            }
        }
        return 0;
    }
}
