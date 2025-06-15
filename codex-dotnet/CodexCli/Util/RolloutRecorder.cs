using System.Text.Json;
using CodexCli.Config;
using CodexCli.Models;

namespace CodexCli.Util;

public class RolloutRecorder : IAsyncDisposable
{
    private readonly StreamWriter _writer;

    private RolloutRecorder(StreamWriter writer)
    {
        _writer = writer;
    }

    public static async Task<RolloutRecorder> CreateAsync(AppConfig cfg, string sessionId, string? instructions)
    {
        var dir = Path.Combine(cfg.CodexHome, "sessions");
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"rollout-{DateTime.UtcNow:yyyy-MM-ddTHH-mm-ss}-{sessionId}.jsonl");
        var writer = new StreamWriter(File.Open(file, FileMode.Create, FileAccess.Write, FileShare.Read));
        var meta = new { id = sessionId, timestamp = DateTime.UtcNow.ToString("O"), instructions };
        await writer.WriteLineAsync(JsonSerializer.Serialize(meta));
        await writer.FlushAsync();
        return new RolloutRecorder(writer);
    }

    public async Task RecordItemsAsync(IEnumerable<ResponseItem> items)
    {
        foreach (var item in items)
        {
            var json = JsonSerializer.Serialize(item, item.GetType());
            await _writer.WriteLineAsync(json);
        }
        await _writer.FlushAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _writer.FlushAsync();
        await _writer.DisposeAsync();
    }
}
