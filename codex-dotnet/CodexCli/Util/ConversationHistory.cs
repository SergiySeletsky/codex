// Ported from codex-rs/core/src/conversation_history.rs (done)
using CodexCli.Models;

namespace CodexCli.Util;

public class ConversationHistory
{
    private readonly List<ResponseItem> _items = new();

    public IReadOnlyList<ResponseItem> Contents() => _items.ToList();

    public void RecordItems(IEnumerable<ResponseItem> items)
    {
        foreach (var item in items)
            if (IsApiMessage(item))
                _items.Add(item);
    }

    private static bool IsApiMessage(ResponseItem item) => item switch
    {
        MessageItem m => m.Role != "system",
        FunctionCallItem => true,
        FunctionCallOutputItem => true,
        LocalShellCallItem => true,
        _ => false
    };

    public ConversationHistory Clone()
    {
        var clone = new ConversationHistory();
        clone._items.AddRange(_items);
        return clone;
    }
}
