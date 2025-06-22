using System.CommandLine;
using CodexCli.Config;

// Ported from codex-rs/cli/src/proto.rs

namespace CodexCli.Commands;

public static class ProtoCommand
{
    public static Command Create(Option<string?> configOption, Option<string?> cdOption)
    {
        var cmd = new Command("proto", "Run protocol mode");
        cmd.SetHandler(async (string? cfgPath, string? cd) =>
        {
            if (cd != null) Environment.CurrentDirectory = cd;
            AppConfig? cfg = null;
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                cfg = AppConfig.Load(cfgPath);

            if (!Console.IsInputRedirected)
            {
                Console.Error.WriteLine("Protocol mode expects stdin to be a pipe.");
                return;
            }

            using var reader = new StreamReader(Console.OpenStandardInput());
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                line = line.Trim();
                if (line.Length == 0) continue;
                try
                {
                    var msg = System.Text.Json.JsonSerializer.Deserialize<CodexCli.Protocol.JsonRpcMessage>(line);
                    if (msg?.Method != null)
                        Console.WriteLine($"method={msg.Method}");
                }
                catch
                {
                    Console.WriteLine($"Invalid JSON: {line}");
                }
            }
        }, configOption, cdOption);
        return cmd;
    }
}
