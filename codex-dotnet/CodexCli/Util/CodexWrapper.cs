using CodexCli.Protocol;

namespace CodexCli.Util;

public static class CodexWrapper
{
    public static async Task<IAsyncEnumerable<Event>> InitCodexAsync(string prompt, OpenAIClient client, string model)
    {
        // For now just forward to RealCodexAgent
        return RealCodexAgent.RunAsync(prompt, client, model);
    }
}
