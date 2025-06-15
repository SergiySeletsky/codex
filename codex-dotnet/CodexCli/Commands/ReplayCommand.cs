using System.CommandLine;
using CodexCli.Util;

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
            await foreach (var line in RolloutReplayer.ReplayLinesAsync(file.FullName))
                Console.WriteLine(line);
        }, fileArg);
        return cmd;
    }
}
