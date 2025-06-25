using Spectre.Console;
using CodexCli.Config;
using CodexCli.Util;
using CodexCli.Protocol;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using CodexCli.Commands;
using SessionManager = CodexCli.Util.SessionManager;

/// <summary>
/// Mirrors codex-rs/tui/src/lib.rs interactive app.
/// Basic event loop with /log and /version commands implemented and
/// session startup routed through CodexWrapper (done). Input queuing
/// via Codex.InjectInput tested in CodexInjectPendingInputTests.
/// </summary>

namespace CodexCli.Interactive;

/// <summary>
/// Shared interactive TUI logic used by Codex CLI and Codex TUI wrappers.
/// </summary>
public static class InteractiveApp
{
    /// <summary>Optional handler for approval requests when running interactively.</summary>
    public static Func<Event, Task<ReviewDecision>>? ApprovalHandler { get; set; }

    public static async Task RunAsync(InteractiveOptions opts, AppConfig? cfg)
    {
        var state = new CodexState();
        bool enableMouse = !(cfg?.Tui.DisableMouseCapture ?? false);
        Console.Write(enableMouse ? "\u001b[?1000h" : "\u001b[?1000l");
        try
        {
            SessionManager.SetPersistence(cfg?.History.Persistence ?? HistoryPersistence.SaveAll);
            var sessionId = SessionManager.CreateSession();
            var history = new List<string>();
            string? lastMessage = null;
            StreamWriter? logWriter = null;
            if (opts.EventLogFile != null)
                logWriter = new StreamWriter(opts.EventLogFile, append: false);
            using var ctrlC = SignalUtils.NotifyOnSigInt();
            CancellationTokenSource? agentCts = null;

            bool withAnsi = !Console.IsOutputRedirected;
            bool hideReason = opts.HideAgentReasoning ?? cfg?.HideAgentReasoning ?? false;
            var processor = new EventProcessor(withAnsi, !hideReason, cfg?.FileOpener ?? UriBasedFileOpener.None, Environment.CurrentDirectory);
            var execPolicy = ExecPolicy.LoadDefault();

            var chat = new ChatWidget();
            using var status = new StatusIndicatorWidget();
            status.Start();
            status.UpdateText("ready");

            chat.AddAgentMessage("Codex interactive mode");
            chat.AddAgentMessage($"Session ID: {sessionId}");

            var providerId = opts.ModelProvider ?? cfg?.ModelProvider;
            if (providerId != null && ModelProviderInfo.BuiltIns.TryGetValue(providerId, out var provInfo))
            {
                if (ApiKeyManager.GetKey(provInfo) == null && provInfo.EnvKey != null)
                {
                    chat.AddAgentMessage($"No API key for {providerId}. Run 'codex provider login {providerId}' to set one.");
                }
            }

            chat.AddAgentMessage("Type /help for commands");
            if (!string.IsNullOrEmpty(opts.Prompt))
            {
                history.Add(opts.Prompt);
                SessionManager.AddEntry(sessionId, opts.Prompt);
                chat.AddUserMessage(opts.Prompt);
            }
            while (true)
            {
                if (ctrlC.IsCancellationRequested)
                {
                    agentCts?.Cancel();
                    Codex.Abort(state);
                    break;
                }
                var prompt = AnsiConsole.Ask<string>("cmd> ");
                if (ctrlC.IsCancellationRequested)
                {
                    agentCts?.Cancel();
                    Codex.Abort(state);
                    break;
                }
                if (prompt.Equals("/quit", StringComparison.OrdinalIgnoreCase))
                    break;
                if (prompt.Equals("/history", StringComparison.OrdinalIgnoreCase))
                {
                    chat.Render(20);
                    continue;
                }
                if (prompt.Equals("/reset", StringComparison.OrdinalIgnoreCase) ||
                    prompt.Equals("/new", StringComparison.OrdinalIgnoreCase))
                {
                    Codex.Abort(state);
                    history.Clear();
                    SessionManager.ClearHistory(sessionId);
                    sessionId = SessionManager.CreateSession();
                    chat.AddAgentMessage($"Started new session {sessionId}");
                    status.UpdateText("new session");
                    continue;
                }
                if (prompt.Equals("/help", StringComparison.OrdinalIgnoreCase))
                {
                    chat.AddAgentMessage("Available commands: /history, /scroll-up, /scroll-down, /new, /toggle-mouse-mode, /quit, /help, /log, /config, /save <file>, /save-last <file>, /version, /sessions, /delete <id>");
                    continue;
                }
                if (prompt.Equals("/log", StringComparison.OrdinalIgnoreCase))
                {
                    var dir = cfg != null ? EnvUtils.GetLogDir(cfg) : Path.Combine(EnvUtils.FindCodexHome(), "log");
                    chat.AddAgentMessage($"Log dir: {dir}");
                    continue;
                }
                if (prompt.Equals("/version", StringComparison.OrdinalIgnoreCase))
                {
                    var ver = typeof(InteractiveApp).Assembly.GetName().Version?.ToString() ?? "?";
                    chat.AddAgentMessage($"Version: {ver}");
                    continue;
                }
                if (prompt.Equals("/sessions", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in SessionManager.ListSessionsWithInfo())
                        chat.AddAgentMessage($"{s.Id} {s.Start:o}");
                    continue;
                }
                if (prompt.StartsWith("/scroll-up", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = prompt.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    int n = parts.Length > 1 && int.TryParse(parts[1], out var val) ? val : 1;
                    chat.ScrollUp(n);
                    chat.Render(20);
                    continue;
                }
                if (prompt.StartsWith("/scroll-down", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = prompt.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    int n = parts.Length > 1 && int.TryParse(parts[1], out var val) ? val : 1;
                    chat.ScrollDown(n);
                    chat.Render(20);
                    continue;
                }
                if (prompt.StartsWith("/delete", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = prompt.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                    {
                        chat.AddAgentMessage("Usage: /delete <id>");
                        continue;
                    }
                    var id = parts[1];
                    if (SessionManager.DeleteSession(id))
                        chat.AddAgentMessage($"Deleted {id}");
                    else
                        chat.AddAgentMessage($"Session {id} not found");
                    continue;
                }
                if (prompt.Equals("/config", StringComparison.OrdinalIgnoreCase))
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
                        if (cfg.ModelReasoningEffort != null)
                            chat.AddAgentMessage($"Reasoning effort: {cfg.ModelReasoningEffort}");
                        if (cfg.ModelReasoningSummary != null)
                            chat.AddAgentMessage($"Reasoning summary: {cfg.ModelReasoningSummary}");
                    }
                    else
                    {
                        chat.AddAgentMessage("No config loaded");
                    }
                    continue;
                }
                if (prompt.StartsWith("/save-last", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = prompt.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2 || lastMessage == null)
                    {
                        chat.AddAgentMessage("Usage: /save-last <file>");
                        continue;
                    }
                    File.WriteAllText(parts[1], lastMessage);
                    chat.AddAgentMessage($"Saved last message to {parts[1]}");
                    continue;
                }
                if (prompt.StartsWith("/save", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = prompt.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                    {
                        chat.AddAgentMessage("Usage: /save <file>");
                        continue;
                    }
                    var file = parts[1];
                    File.WriteAllLines(file, history);
                    chat.AddAgentMessage($"Saved history to {file}");
                    continue;
                }
                if (state.HasCurrentTask &&
                    Codex.InjectInput(state, new List<InputItem>{ new TextInputItem(prompt) }))
                {
                    history.Add(prompt);
                    SessionManager.AddEntry(sessionId, prompt);
                    if (logWriter != null)
                        await logWriter.WriteLineAsync(prompt);
                    chat.AddUserMessage(prompt);
                    status.UpdateText("queued input");
                    continue;
                }

                history.Add(prompt);
                SessionManager.AddEntry(sessionId, prompt);
                if (logWriter != null)
                    await logWriter.WriteLineAsync(prompt);
                lastMessage = prompt;
                chat.AddUserMessage(prompt);
                status.UpdateText("thinking...");

                var info = cfg?.GetProvider(providerId ?? "openai") ?? ModelProviderInfo.BuiltIns[providerId ?? "openai"];
                var client = new OpenAIClient(ApiKeyManager.GetKey(info), info.BaseUrl);

                async Task<ReviewDecision> DefaultApproval(Event ev)
                {
                    if (ev is ExecApprovalRequestEvent execReq)
                    {
                        if (execPolicy.IsForbidden(execReq.Command.First()))
                        {
                            chat.AddAgentMessage($"Denied '{string.Join(" ", execReq.Command)}' ({execPolicy.GetReason(execReq.Command.First())})");
                            return ReviewDecision.Denied;
                        }
                        if (!execPolicy.VerifyCommand(execReq.Command.First(), execReq.Command.Skip(1)))
                        {
                            chat.AddAgentMessage($"Denied '{string.Join(" ", execReq.Command)}' (unverified)");
                            return ReviewDecision.Denied;
                        }
                        chat.AddAgentMessage($"Run '{string.Join(" ", execReq.Command)}'? [y/N]");
                        var resp = Console.ReadLine();
                        var dec = resp?.StartsWith("y", StringComparison.OrdinalIgnoreCase) == true ? ReviewDecision.Approved : ReviewDecision.Denied;
                        chat.AddAgentMessage(dec == ReviewDecision.Approved ? "Approved" : "Denied");
                        return dec;
                    }
                    if (ev is PatchApplyApprovalRequestEvent patchReq)
                    {
                        chat.AddAgentMessage($"Apply patch? [y/N] {patchReq.PatchSummary}");
                        var r = Console.ReadLine();
                        var dec = r?.StartsWith("y", StringComparison.OrdinalIgnoreCase) == true ? ReviewDecision.Approved : ReviewDecision.Denied;
                        chat.AddAgentMessage(dec == ReviewDecision.Approved ? "Approved" : "Denied");
                        return dec;
                    }
                    return ReviewDecision.Denied;
                }

                var approvalHandler = ApprovalHandler ?? DefaultApproval;

                Func<string, OpenAIClient, string, CancellationToken, IAsyncEnumerable<Event>> factory = info.Name == "Mock"
                    ? (p, c, m, t) => CodexCli.Protocol.MockCodexAgent.RunAsync(p, Array.Empty<string>(), approvalHandler, t)
                    : (p, c, m, t) => CodexCli.Protocol.RealCodexAgent.RunAsync(p, c, m, approvalHandler, Array.Empty<string>(), opts.NotifyCommand, t);

                var (stream, first, cts) = await CodexWrapper.InitCodexAsync(prompt, client, opts.Model ?? cfg?.Model ?? "default", factory, opts.NotifyCommand);
                agentCts = cts;

                processor.ProcessEvent(first);
                await foreach (var ev in stream)
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
                        case ExecCommandBeginEvent beginEv:
                            chat.AddSystemMessage($"exec {string.Join(" ", beginEv.Command)} in {beginEv.Cwd}");
                            break;
                        case ExecCommandEndEvent endEv:
                            chat.AddSystemMessage($"exec {(endEv.ExitCode == 0 ? "succeeded" : $"exited {endEv.ExitCode}")}");
                            break;
                        case PatchApplyBeginEvent pb:
                            chat.AddSystemMessage($"apply_patch auto_approved={pb.AutoApproved}");
                            break;
                        case PatchApplyEndEvent pe:
                            chat.AddSystemMessage($"apply_patch {(pe.Success ? "succeeded" : "failed")}");
                            break;
                        case McpToolCallBeginEvent mc:
                            chat.AddSystemMessage($"tool {mc.Server}.{mc.Tool}");
                            break;
                        case McpToolCallEndEvent mce:
                            chat.AddSystemMessage($"tool {(mce.IsSuccess ? "success" : "failed")}");
                            break;
                        case AgentReasoningEvent ar when !hideReason:
                            chat.AddSystemMessage(ar.Text);
                            break;
                        case TaskStartedEvent ts:
                            Codex.SetTask(state, new AgentTask(ts.Id, () => agentCts?.Cancel()));
                            break;
                        case TaskCompleteEvent tc:
                        if (tc.LastAgentMessage != null)
                        {
                            chat.AddAgentMessage(tc.LastAgentMessage);
                            lastMessage = tc.LastAgentMessage;
                        }
                        status.UpdateText("ready");
                        Codex.RemoveTask(state, tc.Id);
                        if (opts.NotifyCommand.Length > 0)
                            Codex.MaybeNotify(opts.NotifyCommand.ToList(),
                                new AgentTurnCompleteNotification(tc.Id, Array.Empty<string>(), tc.LastAgentMessage));
                        break;
                    }
                    if (logWriter != null)
                        await logWriter.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(ev));
                }
                Codex.Abort(state);
                agentCts.Dispose();
                agentCts = null;
                }
            if (logWriter != null)
            {
                logWriter.Flush();
                logWriter.Dispose();
            }
            if (SessionManager.GetHistoryFile(sessionId) is { } path)
                chat.AddAgentMessage($"History saved to {path}");
            if (opts.LastMessageFile != null && lastMessage != null)
                File.WriteAllText(opts.LastMessageFile, lastMessage);
        }
        finally
        {
            Codex.Abort(state);
            Console.Write("\u001b[?1000l");
        }
    }
}
