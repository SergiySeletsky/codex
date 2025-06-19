using System;
using System.Collections.Generic;
using System.Linq;
using Spectre.Console;

namespace CodexCli.Interactive;

/// <summary>
/// Popup widget displaying available slash commands.
/// Mirrors codex-rs/tui/src/bottom_pane/command_popup.rs (rendering done).
/// </summary>
public class CommandPopup
{
    private const int MaxPopupRows = 5;
    private string _filter = string.Empty;
    private readonly Dictionary<string, SlashCommand> _allCommands = SlashCommandBuiltIns.All;
    private int? _selectedIdx;

    public void OnComposerTextChange(string text)
    {
        var line = text.Split('\n').FirstOrDefault() ?? string.Empty;
        if (line.StartsWith('/'))
        {
            var token = line.Substring(1).TrimStart().Split(' ', 2)[0];
            _filter = token;
        }
        else
        {
            _filter = string.Empty;
        }
        var count = GetFilteredCommands().Count;
        _selectedIdx = count == 0 ? null : Math.Min(_selectedIdx ?? 0, count - 1);
    }

    public int CalculateRequiredHeight(int areaHeight)
    {
        var count = GetFilteredCommands().Count;
        var rows = Math.Clamp(count, 1, MaxPopupRows);
        return rows + 2; // account for border
    }

    public IReadOnlyList<SlashCommand> GetFilteredCommands()
    {
        var cmds = _allCommands.Values
            .Where(c => string.IsNullOrEmpty(_filter) || c.Command().StartsWith(_filter, StringComparison.OrdinalIgnoreCase))
            .ToList();
        cmds.Sort((a,b) => string.Compare(a.Command(), b.Command(), StringComparison.Ordinal));
        return cmds;
    }

    public void MoveUp()
    {
        var list = GetFilteredCommands();
        if (list.Count == 0) { _selectedIdx = null; return; }
        if (_selectedIdx == null) _selectedIdx = 0;
        else if (_selectedIdx > 0) _selectedIdx--;
    }

    public void MoveDown()
    {
        var list = GetFilteredCommands();
        if (list.Count == 0) { _selectedIdx = null; return; }
        if (_selectedIdx == null) _selectedIdx = 0;
        else if (_selectedIdx + 1 < list.Count) _selectedIdx++;
    }

    public SlashCommand? SelectedCommand()
    {
        var list = GetFilteredCommands();
        if (_selectedIdx is int idx && idx < list.Count) return list[idx];
        return null;
    }

    public void Render()
    {
        var matches = GetFilteredCommands().Take(MaxPopupRows).ToList();
        foreach (var (cmd, idx) in matches.Select((c,i)=>(c,i)))
        {
            var prefix = _selectedIdx == idx ? "[blue]>[/]" : " ";
            AnsiConsole.MarkupLine($"{prefix} /{cmd.Command(),-15} {cmd.Description()}");
        }
    }
}
