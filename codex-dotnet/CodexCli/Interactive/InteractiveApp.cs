using Spectre.Console;
using CodexCli.Config;
using CodexCli.Util;
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
    public static void Run(InteractiveOptions opts, AppConfig? cfg)
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
            AnsiConsole.MarkupLine("[green]Codex interactive mode[/]");
            AnsiConsole.MarkupLine($"Session ID: [yellow]{sessionId}[/]");
            AnsiConsole.MarkupLine("Type /help for commands");
            if (!string.IsNullOrEmpty(opts.Prompt))
            {
                history.Add(opts.Prompt);
                SessionManager.AddEntry(sessionId, opts.Prompt);
                AnsiConsole.MarkupLine($"Initial prompt: [blue]{opts.Prompt}[/]");
            }
            while (true)
            {
                var prompt = AnsiConsole.Ask<string>("cmd> ");
                if (prompt.Equals("/quit", StringComparison.OrdinalIgnoreCase))
                    break;
                if (prompt.Equals("/history", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var item in history)
                        AnsiConsole.MarkupLine($"[blue]{item}[/]");
                    continue;
                }
                if (prompt.Equals("/reset", StringComparison.OrdinalIgnoreCase) ||
                    prompt.Equals("/new", StringComparison.OrdinalIgnoreCase))
                {
                    history.Clear();
                    SessionManager.ClearHistory(sessionId);
                    sessionId = SessionManager.CreateSession();
                    AnsiConsole.MarkupLine($"Started new session [yellow]{sessionId}[/]");
                    continue;
                }
                if (prompt.Equals("/help", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLine("Available commands: /history, /new, /quit, /help, /log, /config, /save <file>, /save-last <file>, /version, /sessions, /delete <id>");
                    continue;
                }
                if (prompt.Equals("/log", StringComparison.OrdinalIgnoreCase))
                {
                    var dir = cfg != null ? EnvUtils.GetLogDir(cfg) : Path.Combine(EnvUtils.FindCodexHome(), "log");
                    AnsiConsole.MarkupLine($"Log dir: [blue]{dir}[/]");
                    continue;
                }
                if (prompt.Equals("/version", StringComparison.OrdinalIgnoreCase))
                {
                    var ver = typeof(InteractiveApp).Assembly.GetName().Version?.ToString() ?? "?";
                    AnsiConsole.MarkupLine($"Version: [blue]{ver}[/]");
                    continue;
                }
                if (prompt.Equals("/sessions", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var info in SessionManager.ListSessionsWithInfo())
                        AnsiConsole.MarkupLine($"{info.Id} {info.Start:o}");
                    continue;
                }
                if (prompt.StartsWith("/delete", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = prompt.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                    {
                        AnsiConsole.MarkupLine("Usage: /delete <id>");
                        continue;
                    }
                    var id = parts[1];
                    if (SessionManager.DeleteSession(id))
                        AnsiConsole.MarkupLine($"Deleted {id}");
                    else
                        AnsiConsole.MarkupLine($"Session {id} not found");
                    continue;
                }
                if (prompt.Equals("/config", StringComparison.OrdinalIgnoreCase))
                {
                    if (cfg != null)
                    {
                        AnsiConsole.MarkupLine($"Model: [blue]{cfg.Model}[/]");
                        if (!string.IsNullOrEmpty(cfg.ModelProvider))
                            AnsiConsole.MarkupLine($"Provider: [blue]{cfg.ModelProvider}[/]");
                        var codexHome = cfg.CodexHome ?? EnvUtils.FindCodexHome();
                        AnsiConsole.MarkupLine($"CodexHome: [blue]{codexHome}[/]");
                        AnsiConsole.MarkupLine($"Hide reasoning: [blue]{cfg.HideAgentReasoning}[/]");
                        AnsiConsole.MarkupLine($"Disable storage: [blue]{cfg.DisableResponseStorage}[/]");
                        if (cfg.ModelReasoningEffort != null)
                            AnsiConsole.MarkupLine($"Reasoning effort: [blue]{cfg.ModelReasoningEffort}[/]");
                        if (cfg.ModelReasoningSummary != null)
                            AnsiConsole.MarkupLine($"Reasoning summary: [blue]{cfg.ModelReasoningSummary}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("No config loaded");
                    }
                    continue;
                }
                if (prompt.StartsWith("/save-last", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = prompt.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2 || lastMessage == null)
                    {
                        AnsiConsole.MarkupLine("Usage: /save-last <file>");
                        continue;
                    }
                    File.WriteAllText(parts[1], lastMessage);
                    AnsiConsole.MarkupLine($"Saved last message to [green]{parts[1]}[/]");
                    continue;
                }
                if (prompt.StartsWith("/save", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = prompt.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2)
                    {
                        AnsiConsole.MarkupLine("Usage: /save <file>");
                        continue;
                    }
                    var file = parts[1];
                    File.WriteAllLines(file, history);
                    AnsiConsole.MarkupLine($"Saved history to [green]{file}[/]");
                    continue;
                }
                history.Add(prompt);
                SessionManager.AddEntry(sessionId, prompt);
                if (logWriter != null)
                    logWriter.WriteLine(prompt);
                lastMessage = prompt;
                AnsiConsole.MarkupLine($"You typed: [blue]{prompt}[/]");
            }
            if (logWriter != null)
            {
                logWriter.Flush();
                logWriter.Dispose();
            }
            if (SessionManager.GetHistoryFile(sessionId) is { } path)
                AnsiConsole.MarkupLine($"History saved to [green]{path}[/]");
            if (opts.LastMessageFile != null && lastMessage != null)
                File.WriteAllText(opts.LastMessageFile, lastMessage);
        }
        finally
        {
            Console.Write("\u001b[?1000l");
        }
    }
}
