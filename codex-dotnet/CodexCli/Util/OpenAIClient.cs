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
}
