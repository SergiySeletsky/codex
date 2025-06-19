using CodexCli.Config;
using CodexCli.Commands;
using CodexCli.Interactive;
using CodexCli.Protocol;
using CodexCli.Util;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Spectre.Console;

namespace CodexTui;

/// <summary>
/// Initial prototype TUI host wiring the BottomPane for user input.
/// Mirrors codex-rs/tui/src/app.rs (login and git warning screens, slash
/// commands, approval overlay, status indicator, history log bridging with
/// PNG/JPEG dimension parsing, and initial/interactive image prompts all
/// implemented. Layout spacing and height clamping done with scroll wheel
/// debouncing via <see cref="ScrollEventHelper"/> and xterm mouse sequence
/// parsing via <see cref="AnsiMouseParser"/> (done; more polish pending).
/// </summary>
internal static class TuiApp
{
    public static async Task<int> RunAsync(InteractiveOptions opts, AppConfig? cfg)
    {
        var queue = new ConcurrentQueue<Event>();
        var sender = new AppEventSender(ev => queue.Enqueue(ev));
        var chat = new ChatWidget(sender);
        var scrollHelper = new ScrollEventHelper(sender);
        var mouseParser = new AnsiMouseParser(scrollHelper);
        using var mouse = new MouseCapture(!(cfg?.Tui.DisableMouseCapture ?? false));
        LogBridge.LatestLog += chat.UpdateLatestLog;

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
        foreach (var img in opts.Images)
        {
            chat.AddUserImage(img.FullName);
        }

        string providerId = opts.ModelProvider ?? cfg?.ModelProvider ?? "Mock";
        bool hideReason = opts.HideAgentReasoning ?? cfg?.HideAgentReasoning ?? false;
        var processor = new EventProcessor(withAnsi: !Console.IsOutputRedirected,
            showReasoning: !hideReason,
            UriBasedFileOpener.None,
            Environment.CurrentDirectory);

        if (!string.IsNullOrEmpty(opts.Prompt) || opts.Images.Length > 0)
        {
            chat.SetTaskRunning(true);
            LogBridge.Emit("thinking...");
            var images = opts.Images.Select(i => i.FullName).ToArray();
            var events = providerId == "Mock"
                ? MockCodexAgent.RunAsync(opts.Prompt ?? string.Empty, images, InteractiveApp.ApprovalHandler)
                : RealCodexAgent.RunAsync(opts.Prompt ?? string.Empty,
                    new OpenAIClient(ApiKeyManager.GetKey(ModelProviderInfo.BuiltIns[providerId]),
                        ModelProviderInfo.BuiltIns[providerId].BaseUrl),
                    opts.Model ?? cfg?.Model ?? "default",
                    InteractiveApp.ApprovalHandler ?? (_ => Task.FromResult(ReviewDecision.Approved)),
                    images);
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
                        chat.AddBackgroundEvent(bg.Message);
                        LogBridge.Emit(bg.Message);
                        break;
                    case AgentReasoningEvent ar:
                        if (!hideReason)
                        {
                            chat.AddAgentReasoning(ar.Text);
                            LogBridge.Emit(ar.Text);
                        }
                        break;
                    case ErrorEvent err:
                        chat.AddError(err.Message);
                        LogBridge.Emit(err.Message);
                        break;
                    case TaskCompleteEvent tc:
                        if (tc.LastAgentMessage != null)
                            chat.AddAgentMessage(tc.LastAgentMessage);
                        chat.SetTaskRunning(false);
                        LogBridge.Emit("ready");
                        break;
                    case McpToolCallBeginEvent mc:
                        chat.AddMcpToolCallBegin(mc.Server, mc.Tool, mc.ArgumentsJson);
                        LogBridge.Emit(mc.ArgumentsJson ?? "");
                        break;
                    case McpToolCallEndEvent mce:
                        if (ToolResultUtils.HasImageOutput(mce.ResultJson))
                            chat.AddMcpToolCallImage(mce.ResultJson);
                        else
                            chat.AddMcpToolCallEnd(mce.IsSuccess, mce.ResultJson);
                        LogBridge.Emit(mce.ResultJson);
                        break;
                }
            }
        }

        InteractiveApp.ApprovalHandler = ev => Task.FromResult(chat.PushApprovalRequest(ev));

        try
        {
        while (true)
        {
            bool scrolled = false;
            while (queue.TryDequeue(out var ev))
            {
                if (ev is ScrollEvent se)
                {
                    chat.HandleScrollDelta(se.Delta);
                    scrolled = true;
                }
            }
            if (scrolled)
                chat.Render(Console.WindowHeight);

            var key = Console.ReadKey(intercept: true);
            if (mouseParser.ProcessChar(key.KeyChar))
                continue;
            var res = chat.HandleKeyEvent(key);
            chat.Render(Console.WindowHeight);
            if (res.IsSubmitted)
            {
                var text = res.SubmittedText!;
                // message already recorded in history by the composer

                if (text.Equals("/quit", StringComparison.OrdinalIgnoreCase))
                    break;

                if (text.Equals("/new", StringComparison.OrdinalIgnoreCase))
                {
                    chat.ClearConversation();
                    continue;
                }

                if (text.Equals("/toggle-mouse-mode", StringComparison.OrdinalIgnoreCase))
                {
                    mouse.Toggle();
                    chat.AddSystemMessage(mouse.IsActive ? "Mouse mode enabled" : "Mouse mode disabled");
                    continue;
                }

                if (text.Equals("/help", StringComparison.OrdinalIgnoreCase))
                {
                    chat.AddAgentMessage("Available commands: /new, /toggle-mouse-mode, /history, /scroll-up, /scroll-down, /sessions, /config, /image <file>, /quit");
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
                    for (int i = 0; i < n; i++) scrollHelper.ScrollUp();
                    await Task.Delay(150);
                    continue;
                }
                if (text.StartsWith("/scroll-down", StringComparison.OrdinalIgnoreCase))
                {
                    int n = ParseIntArg(text);
                    for (int i = 0; i < n; i++) scrollHelper.ScrollDown();
                    await Task.Delay(150);
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

                if (text.StartsWith("/image ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                    {
                        chat.AddError("Usage: /image <file>");
                        continue;
                    }
                    var path = parts[1];
                    if (!File.Exists(path))
                    {
                        chat.AddError($"File not found: {path}");
                        continue;
                    }
                    chat.AddUserImage(path);
                    chat.SetTaskRunning(true);
                    LogBridge.Emit("thinking...");
                    var images = new[] { path };
                    var events = providerId == "Mock"
                        ? MockCodexAgent.RunAsync(string.Empty, images, InteractiveApp.ApprovalHandler)
                        : RealCodexAgent.RunAsync(string.Empty,
                            new OpenAIClient(ApiKeyManager.GetKey(ModelProviderInfo.BuiltIns[providerId]),
                                ModelProviderInfo.BuiltIns[providerId].BaseUrl),
                            opts.Model ?? cfg?.Model ?? "default",
                            InteractiveApp.ApprovalHandler ?? (_ => Task.FromResult(ReviewDecision.Approved)),
                            images);
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
                                chat.AddBackgroundEvent(bg.Message);
                                LogBridge.Emit(bg.Message);
                                break;
                            case AgentReasoningEvent ar:
                                if (!hideReason)
                                {
                                    chat.AddAgentReasoning(ar.Text);
                                    LogBridge.Emit(ar.Text);
                                }
                                break;
                            case ErrorEvent err:
                                chat.AddError(err.Message);
                                LogBridge.Emit(err.Message);
                                break;
                            case TaskCompleteEvent tc:
                                if (tc.LastAgentMessage != null)
                                    chat.AddAgentMessage(tc.LastAgentMessage);
                                chat.SetTaskRunning(false);
                                LogBridge.Emit("ready");
                                break;
                            case McpToolCallBeginEvent mc:
                                chat.AddMcpToolCallBegin(mc.Server, mc.Tool, mc.ArgumentsJson);
                                LogBridge.Emit(mc.ArgumentsJson ?? string.Empty);
                                break;
                            case McpToolCallEndEvent mce:
                                if (ToolResultUtils.HasImageOutput(mce.ResultJson))
                                    chat.AddMcpToolCallImage(mce.ResultJson);
                                else
                                    chat.AddMcpToolCallEnd(mce.IsSuccess, mce.ResultJson);
                                LogBridge.Emit(mce.ResultJson);
                                break;
                        }
                    }
                    continue;
                }

                history.Add(text);
                SessionManager.AddEntry(sessionId, text);

                chat.SetTaskRunning(true);
                LogBridge.Emit("thinking...");

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
                            chat.AddBackgroundEvent(bg.Message);
                            LogBridge.Emit(bg.Message);
                            break;
                        case AgentReasoningEvent ar:
                            if (!hideReason)
                            {
                                chat.AddAgentReasoning(ar.Text);
                                LogBridge.Emit(ar.Text);
                            }
                            break;
                        case ErrorEvent err:
                            chat.AddError(err.Message);
                            LogBridge.Emit(err.Message);
                            break;
                        case TaskCompleteEvent tc:
                            if (tc.LastAgentMessage != null)
                                chat.AddAgentMessage(tc.LastAgentMessage);
                            chat.SetTaskRunning(false);
                            LogBridge.Emit("ready");
                            break;
                        case GetHistoryEntryResponseEvent ge:
                            chat.OnHistoryEntryResponse(ge.SessionId, ge.Offset, ge.Entry);
                            if (ge.Entry != null)
                            {
                                chat.AddHistoryEntry(ge.Offset, ge.Entry);
                                LogBridge.Emit(ge.Entry);
                            }
                            break;
                        case AddToHistoryEvent ah:
                            chat.AddHistoryEntry(0, ah.Text);
                            LogBridge.Emit(ah.Text);
                            break;
                        case ExecCommandBeginEvent begin:
                            chat.AddExecCommand(string.Join(" ", begin.Command));
                            LogBridge.Emit(string.Join(" ", begin.Command));
                            break;
                        case ExecCommandEndEvent end:
                            chat.AddExecResult(end.ExitCode);
                            LogBridge.Emit(end.ExitCode == 0 ? end.Stdout : end.Stderr);
                            break;
                        case McpToolCallBeginEvent mc:
                            chat.AddMcpToolCallBegin(mc.Server, mc.Tool, mc.ArgumentsJson);
                            LogBridge.Emit(mc.ArgumentsJson ?? "");
                            break;
                        case McpToolCallEndEvent mce:
                            if (ToolResultUtils.HasImageOutput(mce.ResultJson))
                                chat.AddMcpToolCallImage(mce.ResultJson);
                            else
                                chat.AddMcpToolCallEnd(mce.IsSuccess, mce.ResultJson);
                            LogBridge.Emit(mce.ResultJson);
                            break;
                        case PatchApplyBeginEvent pb:
                            chat.AddPatchApplyBegin(pb.AutoApproved, pb.Changes);
                            LogBridge.Emit($"patch auto_approved={pb.AutoApproved}");
                            break;
                        case PatchApplyEndEvent pe:
                            chat.AddPatchApplyEnd(pe.Success);
                            LogBridge.Emit(pe.Success ? pe.Stdout : pe.Stderr);
                            break;
                        case SessionConfiguredEvent sc:
                            chat.SetHistoryMetadata(sc.SessionId, 0);
                            break;
                    }
                }

                if (chat.HasActiveView)
                {
                    chat.Render(Console.WindowHeight);
                }
            }

            chat.Render(Console.WindowHeight);
        }
        finally
        {
            LogBridge.LatestLog -= chat.UpdateLatestLog;
            InteractiveApp.ApprovalHandler = null;
        }
        return 0;
    }

    private static int ParseIntArg(string text)
    {
        var parts = text.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 && int.TryParse(parts[1], out var val) ? val : 1;
    }

    private static bool IsImageResult(string json) => ToolResultUtils.HasImageOutput(json);
}
