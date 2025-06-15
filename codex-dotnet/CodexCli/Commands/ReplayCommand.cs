using System.CommandLine;
using CodexCli.Util;
using CodexCli.Models;

namespace CodexCli.Commands;

public static class ReplayCommand
{
    public static Command Create()
    {
        var fileArg = new Argument<FileInfo>("file", "Rollout JSONL file to replay");
        var cmd = new Command("replay", "Replay a rollout conversation")
        {
            fileArg
        };
        cmd.SetHandler(async (FileInfo file) =>
        {
            await foreach (var item in RolloutReplayer.ReplayAsync(file.FullName))
            {
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
        }, fileArg);
        return cmd;
    }
}
