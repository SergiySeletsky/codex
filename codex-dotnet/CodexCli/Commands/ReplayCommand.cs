using System.CommandLine;
using CodexCli.Util;
using CodexCli.Models;

namespace CodexCli.Commands;

public static class ReplayCommand
{
    public static Command Create()
    {
        var fileArg = new Argument<FileInfo?>("file", () => null, "Rollout JSONL file to replay");
        var jsonOpt = new Option<bool>("--json", description: "Output JSON lines");
        var messagesOpt = new Option<bool>("--messages-only", description: "Only print assistant and user messages");
        var startOpt = new Option<int?>("--start-index", "Start index of messages to replay");
        var endOpt = new Option<int?>("--end-index", "End index of messages to replay (inclusive)");
        var roleOpt = new Option<string?>("--role", "Filter messages by role");
        var sessionOpt = new Option<string?>("--session", "Session id to replay");
        var followOpt = new Option<bool>("--follow", "Follow file for new lines");
        var cmd = new Command("replay", "Replay a rollout conversation")
        {
            fileArg
        };
        cmd.AddOption(jsonOpt);
        cmd.AddOption(messagesOpt);
        cmd.AddOption(startOpt);
        cmd.AddOption(endOpt);
        cmd.AddOption(roleOpt);
        cmd.AddOption(sessionOpt);
        cmd.AddOption(followOpt);
        cmd.SetHandler(async (FileInfo? file, bool json, bool messagesOnly, int? startIndex, int? endIndex, string? role, string? session, bool follow) =>
        {
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
            if (path == null) { Console.Error.WriteLine("File or --session required"); return; }
            int index = 0;
            await foreach (var item in RolloutReplayer.ReplayAsync(path, follow))
            {
                if (startIndex != null && index < startIndex.Value) { index++; continue; }
                if (endIndex != null && index > endIndex.Value) break;
                if (json)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(item, item.GetType()));
                    index++; continue;
                }

                if (messagesOnly && item is not MessageItem) { index++; continue; }
                if (role != null && item is MessageItem mm && mm.Role != role) { index++; continue; }

                switch (item)
                {
                    case MessageItem m:
                        Console.WriteLine($"{m.Role}: {string.Join("", m.Content.Select(c => c.Text))}");
                        break;
                    case FunctionCallItem fc:
                        Console.WriteLine($"Function {fc.Name} {fc.Arguments}");
                        break;
                    case FunctionCallOutputItem fo:
                        Console.WriteLine($"Function output {fo.CallId}: {fo.Output.Content}");
                        break;
                    case LocalShellCallItem ls:
                        Console.WriteLine($"Shell {string.Join(' ', ls.Action.Exec.Command)} -> {ls.Status}");
                        break;
                    case ReasoningItem ri:
                        Console.WriteLine($"Reasoning: {string.Join(" ", ri.Summary.Select(s => s.Text))}");
                        break;
                    default:
                        Console.WriteLine(item.GetType().Name);
                        break;
                }
                index++;
            }
        }, fileArg, jsonOpt, messagesOpt, startOpt, endOpt, roleOpt, sessionOpt, followOpt);
        return cmd;
    }
}
