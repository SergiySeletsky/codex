using System.Text.Json;
using CodexCli.Models;
using CodexCli.Commands;
using System.Linq;

/// <summary>
/// Port of codex-rs/core/src/client_common.rs ModelClient logic (done).
/// </summary>
namespace CodexCli.Util;

public class ModelClient
{
    private readonly OpenAIClient _client;
    private readonly string _model;
    private readonly ReasoningEffort _effort;
    private readonly ReasoningSummary _summary;

    public ModelClient(string model, OpenAIClient client, ReasoningEffort effort, ReasoningSummary summary)
    {
        _model = model;
        _client = client;
        _effort = effort;
        _summary = summary;
    }

    public async Task<ResponseStream> StreamAsync(CodexCli.Models.Prompt prompt)
    {
        var stream = new ResponseStream();
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var chunk in _client.ChatStreamAsync(prompt.Input.FirstOrDefault() is MessageItem m ? m.Content.First().Text : string.Empty))
                {
                    stream.Writer.TryWrite(new OutputItemDone(new MessageItem("assistant", new List<ContentItem>{ new("output_text", chunk) })));
                }
                stream.Writer.TryWrite(new Completed(Guid.NewGuid().ToString()));
            }
            catch (Exception ex)
            {
                stream.Writer.TryWrite(new OutputItemDone(new MessageItem("assistant", new List<ContentItem>{ new("output_text", $"error: {ex.Message}") })));
                stream.Writer.TryWrite(new Completed(Guid.NewGuid().ToString()));
            }
            stream.Writer.Complete();
        });
        return ChatCompletions.Aggregate(stream);
    }
}
