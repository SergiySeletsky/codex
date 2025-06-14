namespace CodexCli.Util;

public class OpenAIClient
{
    private readonly string? _apiKey;

    public OpenAIClient(string? apiKey)
    {
        _apiKey = apiKey;
    }

    public async Task<string> ChatAsync(string prompt)
    {
        // placeholder for real OpenAI call
        await Task.Delay(10);
        return $"response to '{prompt}'";
    }
}
