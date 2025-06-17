using CodexCli.Config;
using CodexCli.Commands;
using CodexCli.Interactive;
using CodexCli.Protocol;
using CodexCli.Util;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Spectre.Console;

namespace CodexTui;

/// <summary>
/// Initial prototype TUI host wiring the BottomPane for user input.
/// Mirrors codex-rs/tui/src/app.rs (slash commands implemented, widgets in progress).
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

        var sessionId = SessionManager.CreateSession();
        var history = new List<string>();
        string? lastMessage = null;

        chat.AddAgentMessage("Codex TUI prototype - type /quit to exit");
        if (!string.IsNullOrEmpty(opts.Prompt))
        {
            chat.AddUserMessage(opts.Prompt);
            history.Add(opts.Prompt);
            SessionManager.AddEntry(sessionId, opts.Prompt);
        }

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
            pane.Render(3);
            if (res.IsSubmitted)
            {
                var text = res.SubmittedText!;
                chat.AddUserMessage(text);

                if (text.Equals("/quit", StringComparison.OrdinalIgnoreCase))
                    break;

                if (text.Equals("/help", StringComparison.OrdinalIgnoreCase))
                {
                    chat.AddAgentMessage("Available commands: /history, /scroll-up, /scroll-down, /sessions, /config, /quit");
                    continue;
                }
                if (text.Equals("/history", StringComparison.OrdinalIgnoreCase))
                {
                    chat.Render(20);
                    continue;
                }
                if (text.StartsWith("/scroll-up", StringComparison.OrdinalIgnoreCase))
                {
                    int n = ParseIntArg(text);
                    chat.ScrollUp(n);
                    chat.Render(20);
                    continue;
                }
                if (text.StartsWith("/scroll-down", StringComparison.OrdinalIgnoreCase))
                {
                    int n = ParseIntArg(text);
                    chat.ScrollDown(n);
                    chat.Render(20);
                    continue;
                }
                if (text.Equals("/sessions", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in SessionManager.ListSessionsWithInfo())
                        chat.AddAgentMessage($"{s.Id} {s.Start:o}");
                    continue;
                }
                if (text.Equals("/config", StringComparison.OrdinalIgnoreCase))
                {
                    if (cfg != null)
                    {
                        chat.AddAgentMessage($"Model: {cfg.Model}");
                        if (!string.IsNullOrEmpty(cfg.ModelProvider))
                            chat.AddAgentMessage($"Provider: {cfg.ModelProvider}");
                        var codexHome = cfg.CodexHome ?? EnvUtils.FindCodexHome();
                        chat.AddAgentMessage($"CodexHome: {codexHome}");
                        chat.AddAgentMessage($"Hide reasoning: {cfg.HideAgentReasoning}");
                        chat.AddAgentMessage($"Disable storage: {cfg.DisableResponseStorage}");
                    }
                    else
                    {
                        chat.AddAgentMessage("No config loaded");
                    }
                    continue;
                }

                history.Add(text);
                SessionManager.AddEntry(sessionId, text);

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
                            lastMessage = am.Message;
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
                        case GetHistoryEntryResponseEvent ge:
                            pane.OnHistoryEntryResponse(ge.SessionId, ge.Offset, ge.Entry);
                            break;
                        case SessionConfiguredEvent sc:
                            pane.SetHistoryMetadata(sc.SessionId, 0);
                            break;
                    }
                }
                pane.Render(3);
            }
       }
        return 0;
    }

    private static int ParseIntArg(string text)
    {
        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 && int.TryParse(parts[1], out var val) ? val : 1;
    }
}
