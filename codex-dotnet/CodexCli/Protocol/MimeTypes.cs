using System.IO;

namespace CodexCli.Protocol;

// Minimal mime type helper for InputItem.ToResponse
internal static class MimeTypes
{
    public static string GetMimeType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream",
        };
    }
}
