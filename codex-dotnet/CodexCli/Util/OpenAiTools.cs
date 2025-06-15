using System.Text.Json;

namespace CodexCli.Util;

public static class OpenAiTools
{
    public static List<JsonElement> CreateToolsJson(string model, List<(string Name, JsonElement Schema)> extraTools)
    {
        var tools = new List<JsonElement>();
        if (!model.StartsWith("codex"))
        {
            var schema = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new { command = new { type = "array", items = new { type = "string" } }, workdir = new { type = "string" }, timeout = new { type = "number" } },
                required = new[] { "command" },
                additionalProperties = false
            });
            var tool = JsonSerializer.SerializeToElement(new { name = "shell", description = "Runs a shell command", parameters = schema, type = "function" });
            tools.Add(tool);
        }
        foreach (var (name, schema) in extraTools)
        {
            var tool = JsonSerializer.SerializeToElement(new { name, description = "extra tool", parameters = schema, type = "function" });
            tools.Add(tool);
        }
        return tools;
    }
}
