using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodexCli.Protocol;

public class JsonRpcMessage
{
    [JsonPropertyName("jsonrpc")]
    public string Version { get; set; } = "2.0";
    [JsonPropertyName("method")]
    public string? Method { get; set; }
    [JsonPropertyName("params")]
    public JsonElement? Params { get; set; }
    [JsonPropertyName("id")]
    public JsonElement? Id { get; set; }
    [JsonPropertyName("result")]
    public JsonElement? Result { get; set; }
    [JsonPropertyName("error")]
    public JsonElement? Error { get; set; }
}
