using System.CommandLine;
using System.CommandLine.Invocation;
using CodexCli.Util;
using CodexCli.Models;
using Spectre.Console;

namespace CodexCli.Commands;

public static class ReplayCommand
{
    public static Command Create()
    {
        var fileArg = new Argument<FileInfo?>("file", () => null, "Rollout JSONL file to replay");
        var jsonOpt = new Option<bool>("--json", description: "Output JSON lines");
        var messagesOpt = new Option<bool>("--messages-only", description: "Only print assistant and user messages");
        var plainOpt = new Option<bool>("--plain", description: "Disable colored output");
        var compactOpt = new Option<bool>("--compact", description: "Omit line numbers");
        var showSystemOpt = new Option<bool>("--show-system", () => false, "Include system messages");
        var maxItemsOpt = new Option<int?>("--max-items", "Maximum items to output");
        var startOpt = new Option<int?>("--start-index", "Start index of messages to replay");
        var endOpt = new Option<int?>("--end-index", "End index of messages to replay (inclusive)");
        var roleOpt = new Option<string?>("--role", "Filter messages by role");
        var sessionOpt = new Option<string?>("--session", "Session id to replay");
        var latestOpt = new Option<bool>("--latest", "Replay latest session");
        var followOpt = new Option<bool>("--follow", "Follow file for new lines");
        var cmd = new Command("replay", "Replay a rollout conversation")
        {
            fileArg
        };
        cmd.AddOption(jsonOpt);
        cmd.AddOption(messagesOpt);
        cmd.AddOption(showSystemOpt);
        cmd.AddOption(plainOpt);
        cmd.AddOption(compactOpt);
        cmd.AddOption(maxItemsOpt);
        cmd.AddOption(startOpt);
        cmd.AddOption(endOpt);
        cmd.AddOption(roleOpt);
        cmd.AddOption(sessionOpt);
        cmd.AddOption(latestOpt);
        cmd.AddOption(followOpt);
        cmd.SetHandler(async (InvocationContext ctx) =>
        {
            var file = ctx.ParseResult.GetValueForArgument(fileArg);
            var json = ctx.ParseResult.GetValueForOption(jsonOpt);
            var messagesOnly = ctx.ParseResult.GetValueForOption(messagesOpt);
            var showSystem = ctx.ParseResult.GetValueForOption(showSystemOpt);
            var plain = ctx.ParseResult.GetValueForOption(plainOpt);
            var compact = ctx.ParseResult.GetValueForOption(compactOpt);
            var maxItems = ctx.ParseResult.GetValueForOption(maxItemsOpt);
            var startIndex = ctx.ParseResult.GetValueForOption(startOpt);
            var endIndex = ctx.ParseResult.GetValueForOption(endOpt);
            var role = ctx.ParseResult.GetValueForOption(roleOpt);
            var session = ctx.ParseResult.GetValueForOption(sessionOpt);
            var latest = ctx.ParseResult.GetValueForOption(latestOpt);
            var follow = ctx.ParseResult.GetValueForOption(followOpt);

            var path = file?.FullName;
            if (path == null && session != null)
            {
                var dir = Path.Combine(EnvUtils.FindCodexHome(), "sessions");
                path = Directory.GetFiles(dir, $"rollout-*-{session}.jsonl").FirstOrDefault();
                if (path == null)
                {
                    Console.Error.WriteLine($"Session {session} not found");
                    return;
                }
            }
            if (path == null && latest)
            {
                var latestId = SessionManager.GetLatestSessionId();
                if (latestId != null)
                {
                    var dir = Path.Combine(EnvUtils.FindCodexHome(), "sessions");
                    path = Directory.GetFiles(dir, $"rollout-*-{latestId}.jsonl").FirstOrDefault();
                }
            }

            if (path == null) { Console.Error.WriteLine("File or --session/--latest required"); return; }
            int index = 0;
            await foreach (var item in RolloutReplayer.ReplayAsync(path, follow))
            {
                if (startIndex != null && index < startIndex.Value) { index++; continue; }
                if (endIndex != null && index > endIndex.Value) break;
                if (maxItems != null && index - (startIndex ?? 0) >= maxItems.Value) break;
                if (json)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(item, item.GetType()));
                    index++; continue;
                }

                if (messagesOnly && item is not MessageItem) { index++; continue; }
                if (!showSystem && item is MessageItem mm2 && mm2.Role == "system") { index++; continue; }
                if (role != null && item is MessageItem mm && mm.Role != role) { index++; continue; }

                var prefix = compact ? string.Empty : $"{index}: ";
                switch (item)
                {
                    case MessageItem m:
                        var text = string.Join("", m.Content.Select(c => c.Text));
                        if (plain)
                            Console.WriteLine($"{prefix}{m.Role}: {text}");
                        else
                        {
                            var color = m.Role switch { "assistant" => "green", "user" => "yellow", "system" => "grey" , _ => "white" };
                            AnsiConsole.MarkupLine($"{prefix}[{color}]{m.Role}[/]: {Markup.Escape(text)}");
                        }
                        break;
                    case FunctionCallItem fc:
                        Console.WriteLine($"{prefix}Function {fc.Name} {fc.Arguments}");
                        break;
                    case FunctionCallOutputItem fo:
                        Console.WriteLine($"{prefix}Function output {fo.CallId}: {fo.Output.Content}");
                        break;
                    case LocalShellCallItem ls:
                        Console.WriteLine($"{prefix}Shell {string.Join(' ', ls.Action.Exec.Command)} -> {ls.Status}");
                        break;
                    case ReasoningItem ri:
                        Console.WriteLine($"{prefix}Reasoning: {string.Join(" ", ri.Summary.Select(s => s.Text))}");
                        break;
                    default:
                        Console.WriteLine($"{prefix}{item.GetType().Name}");
                        break;
                }
                index++;
            }
        });
        return cmd;
    }
}
