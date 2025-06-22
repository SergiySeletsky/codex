// Rust analog: codex-rs/core/src/protocol.rs InputItem (done)
namespace CodexCli.Protocol;

using System;
using System.Text;
using System.Text.Json.Serialization;
using System.IO;
using CodexCli.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextInputItem), typeDiscriminator: "text")]
[JsonDerivedType(typeof(ImageInputItem), typeDiscriminator: "image")]
[JsonDerivedType(typeof(LocalImageInputItem), typeDiscriminator: "local_image")]
public abstract record InputItem
{
    public static ResponseInputItem ToResponse(IEnumerable<InputItem> items)
    {
        var content = new List<ContentItem>();
        foreach (var item in items)
        {
            switch (item)
            {
                case TextInputItem t:
                    content.Add(new ContentItem("input_text", t.Text));
                    break;
                case ImageInputItem i:
                    content.Add(new ContentItem("input_image", i.ImageUrl));
                    break;
                case LocalImageInputItem li:
                    try
                    {
                        var bytes = File.ReadAllBytes(li.Path);
                        var mime = MimeTypes.GetMimeType(li.Path);
                        var encoded = Convert.ToBase64String(bytes);
                        var dataUrl = $"data:{mime};base64,{encoded}";
                        content.Add(new ContentItem("input_image", dataUrl));
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"Skipping image {li.Path}: {e.Message}");
                    }
                    break;
            }
        }
        return new MessageInputItem("user", content);
    }
}

public record TextInputItem(string Text) : InputItem;
public record ImageInputItem(string ImageUrl) : InputItem;
public record LocalImageInputItem(string Path) : InputItem;
