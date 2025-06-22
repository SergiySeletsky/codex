/// <summary>
/// Port of codex-rs/core/src/client_common.rs Prompt (done).
/// </summary>
namespace CodexCli.Models;

using System.Collections.Generic;
using System.Text;
using System.IO;
using System;
using CodexCli.Util;

public class Prompt
{
    public List<ResponseItem> Input { get; } = new();
    public string? PrevId { get; set; }
    public string? UserInstructions { get; set; }
    public bool Store { get; set; }
    public Dictionary<string, Tool> ExtraTools { get; } = new();

    private static readonly string BaseInstructions = LoadInstructions("prompt.md");

    public string GetFullInstructions(string model)
    {
        var sections = new List<string> { BaseInstructions };
        if (!string.IsNullOrWhiteSpace(UserInstructions))
            sections.Add(UserInstructions);
        if (model.StartsWith("gpt-4.1"))
            sections.Add(ApplyPatchToolInstructions);
        return string.Join('\n', sections);
    }

    private static readonly string ApplyPatchToolInstructions = LoadInstructions(Path.Combine("ApplyPatch", "apply_patch_tool_instructions.md"));

    private static string LoadInstructions(string relative)
    {
        var path = Path.Combine(AppContext.BaseDirectory, relative);
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }
}
