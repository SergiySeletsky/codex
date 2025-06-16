using Spectre.Console;
using CodexCli.Config;
using CodexCli.Util;
using CodexCli.Protocol;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using CodexCli.Commands;
using SessionManager = CodexCli.Util.SessionManager;

namespace CodexCli.Interactive;

/// <summary>
/// Shared interactive TUI logic used by Codex CLI and Codex TUI wrappers.
/// </summary>
public static class InteractiveApp
{
    public static async Task RunAsync(InteractiveOptions opts, AppConfig? cfg)
    {
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
                var prompt = AnsiConsole.Ask<string>("cmd> ");
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
                    history.Clear();
                    SessionManager.ClearHistory(sessionId);
                    sessionId = SessionManager.CreateSession();
                    chat.AddAgentMessage($"Started new session {sessionId}");
                    status.UpdateText("new session");
                    continue;
                }
                if (prompt.Equals("/help", StringComparison.OrdinalIgnoreCase))
                {
                    chat.AddAgentMessage("Available commands: /history, /scroll-up, /scroll-down, /new, /quit, /help, /log, /config, /save <file>, /save-last <file>, /version, /sessions, /delete <id>");
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
                history.Add(prompt);
                SessionManager.AddEntry(sessionId, prompt);
                if (logWriter != null)
                    await logWriter.WriteLineAsync(prompt);
                lastMessage = prompt;
                chat.AddUserMessage(prompt);
                status.UpdateText("thinking...");

                var info = cfg?.GetProvider(providerId ?? "openai") ?? ModelProviderInfo.BuiltIns[providerId ?? "openai"];
                var client = new OpenAIClient(ApiKeyManager.GetKey(info), info.BaseUrl);
                var events = (info.Name == "Mock")
                    ? CodexCli.Protocol.MockCodexAgent.RunAsync(prompt, Array.Empty<string>())
                    : CodexCli.Protocol.RealCodexAgent.RunAsync(prompt, client, opts.Model ?? cfg?.Model ?? "default");
                await foreach (var ev in events)
                {
                    processor.ProcessEvent(ev);
                    switch (ev)
                    {
                        case AgentMessageEvent am:
                            chat.AddAgentMessage(am.Message);
                            lastMessage = am.Message;
                            break;
                        case TaskCompleteEvent tc:
                            if (tc.LastAgentMessage != null)
                            {
                                chat.AddAgentMessage(tc.LastAgentMessage);
                                lastMessage = tc.LastAgentMessage;
                            }
                            status.UpdateText("ready");
                            break;
                        case ExecApprovalRequestEvent ar:
                            if (execPolicy.IsForbidden(ar.Command.First()))
                            {
                                chat.AddAgentMessage($"Denied '{string.Join(" ", ar.Command)}' ({execPolicy.GetReason(ar.Command.First())})");
                            }
                            else if (!execPolicy.VerifyCommand(ar.Command.First(), ar.Command.Skip(1)))
                            {
                                chat.AddAgentMessage($"Denied '{string.Join(" ", ar.Command)}' (unverified)");
                            }
                            else
                            {
                                chat.AddAgentMessage($"Run '{string.Join(" ", ar.Command)}'? [y/N]");
                                var resp = Console.ReadLine();
                                if (resp?.StartsWith("y", StringComparison.OrdinalIgnoreCase) == true)
                                    chat.AddAgentMessage("Approved");
                                else
                                    chat.AddAgentMessage("Denied");
                            }
                            break;
                        case PatchApplyApprovalRequestEvent pr:
                            chat.AddAgentMessage($"Apply patch? [y/N] {pr.PatchSummary}");
                            var r = Console.ReadLine();
                            if (r?.StartsWith("y", StringComparison.OrdinalIgnoreCase) == true)
                                chat.AddAgentMessage("Approved");
                            else
                                chat.AddAgentMessage("Denied");
                            break;
                    }
                    if (logWriter != null)
                        await logWriter.WriteLineAsync(System.Text.Json.JsonSerializer.Serialize(ev));
                }
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
            Console.Write("\u001b[?1000l");
        }
    }
}
