using System.Text.Json;
using CodexCli.Models;

/// <summary>
/// Port of codex-rs/core/src/openai_tools.rs (done).
/// </summary>

namespace CodexCli.Util;

public static class OpenAiTools
{
    private static readonly JsonElement ShellTool = JsonSerializer.SerializeToElement(new
    {
        type = "function",
        function = new
        {
            name = "shell",
            description = "Runs a shell command, and returns its output.",
            strict = false,
            parameters = new
            {
                type = "object",
                properties = new
                {
                    command = new { type = "array", items = new { type = "string" } },
                    workdir = new { type = "string" },
                    timeout = new { type = "number" }
                },
                required = new[] { "command" },
                additionalProperties = false
            }
        }
    });

    private static readonly JsonElement LocalShellTool = JsonSerializer.SerializeToElement(new
    {
        type = "local_shell"
    });

    public static List<JsonElement> CreateToolsJsonForResponsesApi(CodexCli.Models.Prompt prompt, string model)
    {
        var tools = new List<JsonElement>();
        if (model.StartsWith("codex"))
        {
            tools.Add(LocalShellTool);
        }
        else
        {
            tools.Add(ShellTool);
        }

        foreach (var kv in prompt.ExtraTools)
        {
            tools.Add(McpToolToOpenAiTool(kv.Key, kv.Value));
        }

        return tools;
    }

    public static List<JsonElement> CreateToolsJsonForChatCompletionsApi(CodexCli.Models.Prompt prompt, string model)
    {
        var responses = CreateToolsJsonForResponsesApi(prompt, model);
        var tools = new List<JsonElement>();
        foreach (var tool in responses)
        {
            if (tool.ValueKind != JsonValueKind.Object)
                continue;

            if (tool.TryGetProperty("type", out var tProp) && tProp.GetString() == "function")
            {
                if (tool.TryGetProperty("function", out var func))
                {
                    var converted = JsonSerializer.SerializeToElement(new { type = "function", function = func });
                    tools.Add(converted);
                }
            }
        }
        return tools;
    }

    private static JsonElement McpToolToOpenAiTool(string name, Tool tool)
    {
        var schema = tool.InputSchema;
        if (schema.Properties is null)
        {
            schema = schema with { Properties = JsonDocument.Parse("{}").RootElement };
        }

        var element = JsonSerializer.SerializeToElement(new
        {
            name,
            description = tool.Description,
            parameters = new
            {
                type = schema.Type,
                properties = schema.Properties,
                required = schema.Required,
                additionalProperties = false
            },
            type = "function"
        });
        return element;
    }
}
