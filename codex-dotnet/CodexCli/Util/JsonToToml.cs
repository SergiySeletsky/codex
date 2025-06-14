using System.Text.Json;
using Tomlyn.Model;
using Tomlyn;

namespace CodexCli.Util;

public static class JsonToToml
{
    public static string ConvertToToml(JsonElement element)
    {
        var model = Convert(element);
        return Toml.FromModel(model);
    }

    private static object? Convert(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ToTable(element),
            JsonValueKind.Array => ToArray(element),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static TomlTable ToTable(JsonElement obj)
    {
        var table = new TomlTable();
        foreach (var prop in obj.EnumerateObject())
            table[prop.Name] = Convert(prop.Value);
        return table;
    }

    private static TomlArray ToArray(JsonElement arr)
    {
        var a = new TomlArray();
        foreach (var v in arr.EnumerateArray())
            a.Add(Convert(v));
        return a;
    }
}
