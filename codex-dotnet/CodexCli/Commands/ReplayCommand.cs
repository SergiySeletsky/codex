using System.CommandLine;
using CodexCli.Util;
using CodexCli.Models;

namespace CodexCli.Commands;

public static class ReplayCommand
{
    public static Command Create()
    {
        var fileArg = new Argument<FileInfo>("file", "Rollout JSONL file to replay");
        var jsonOpt = new Option<bool>("--json", description: "Output JSON lines");
        var messagesOpt = new Option<bool>("--messages-only", description: "Only print assistant and user messages");
        var cmd = new Command("replay", "Replay a rollout conversation")
        {
            fileArg
        };
        cmd.AddOption(jsonOpt);
        cmd.AddOption(messagesOpt);
        cmd.SetHandler(async (FileInfo file, bool json, bool messagesOnly) =>
        {
            await foreach (var item in RolloutReplayer.ReplayAsync(file.FullName))
            {
                if (json)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(item, item.GetType()));
                    continue;
                }

                if (messagesOnly && item is not MessageItem)
                    continue;

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
            }
        }, fileArg, jsonOpt, messagesOpt);
        return cmd;
    }
}
