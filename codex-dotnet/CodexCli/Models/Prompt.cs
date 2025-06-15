namespace CodexCli.Models;

using System.Collections.Generic;
using System.Text;
using CodexCli.Util;

public class Prompt
{
    public List<ResponseItem> Input { get; } = new();
    public string? PrevId { get; set; }
    public string? UserInstructions { get; set; }
    public bool Store { get; set; }
    public Dictionary<string, Tool> ExtraTools { get; } = new();

    private const string BaseInstructions = "You are Codex, a coding assistant."; // from prompt.md

    public string GetFullInstructions(string model)
    {
        var sections = new List<string> { BaseInstructions };
        if (!string.IsNullOrWhiteSpace(UserInstructions))
            sections.Add(UserInstructions);
        if (model.StartsWith("gpt-4.1"))
            sections.Add(ApplyPatchToolInstructions);
        return string.Join('\n', sections);
    }

    private const string ApplyPatchToolInstructions = "ApplyPatch tool usage";
}
