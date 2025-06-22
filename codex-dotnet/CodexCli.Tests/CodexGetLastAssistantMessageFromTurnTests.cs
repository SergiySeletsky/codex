using CodexCli.Models;
using CodexCli.Util;
using System.Collections.Generic;
using Xunit;

public class CodexGetLastAssistantMessageFromTurnTests
{
    [Fact]
    public void FindsAssistantText()
    {
        var responses = new List<ResponseItem>
        {
            new MessageItem("user", new List<ContentItem>{ new("output_text", "hi") }),
            new MessageItem("assistant", new List<ContentItem>{ new("output_text", "first") }),
            new MessageItem("assistant", new List<ContentItem>{ new("output_text", "second") })
        };
        var result = Codex.GetLastAssistantMessageFromTurn(responses);
        Assert.Equal("second", result);
    }

    [Fact]
    public void ReturnsNullWhenMissing()
    {
        var responses = new List<ResponseItem>
        {
            new MessageItem("user", new List<ContentItem>{ new("output_text", "hi") })
        };
        var result = Codex.GetLastAssistantMessageFromTurn(responses);
        Assert.Null(result);
    }
}
