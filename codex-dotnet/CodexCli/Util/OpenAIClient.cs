using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace CodexCli.Util;

public class OpenAIClient
{
    private readonly string? _apiKey;
    private readonly string _baseUrl;
    public OpenAIClient(string? apiKey, string baseUrl)
    {
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<string> ChatAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("API key not set");
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        var payload = new
        {
            model = "gpt-3.5-turbo",
            messages = new[] { new { role = "user", content = prompt } }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var resp = await http.PostAsync($"{_baseUrl}/chat/completions", content);
        resp.EnsureSuccessStatusCode();
        var respJson = await resp.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(respJson);
        var result = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return result ?? string.Empty;
    }

    public async IAsyncEnumerable<string> ChatStreamAsync(string prompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancel = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("API key not set");
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        var payload = new
        {
            model = "gpt-3.5-turbo",
            stream = true,
            messages = new[] { new { role = "user", content = prompt } }
        };
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions") { Content = content };
        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancel);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(cancel);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream && !cancel.IsCancellationRequested)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync();
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            if (string.IsNullOrWhiteSpace(line) || !line!.StartsWith("data:"))
                continue;
            var data = line.Substring(5).Trim();
            if (data == "[DONE]")
                yield break;
            using var doc = System.Text.Json.JsonDocument.Parse(data);
            var choices = doc.RootElement.GetProperty("choices");
            if (choices.GetArrayLength() > 0)
            {
                var delta = choices[0].GetProperty("delta");
                if (delta.TryGetProperty("content", out var c))
                    yield return c.GetString() ?? string.Empty;
            }
        }
    }
}
