using System.Collections.Generic;
using CodexCli.Interactive;
using Xunit;

public class AnsiMouseParserTests
{
    [Fact(Skip = "pending")]
    public void ParsesUpAndDown()
    {
        var deltas = new List<int>();
        var parser = new AnsiMouseParser();
        string seq = "\u001b[<64;0;0M\u001b[<65;0;0M";
        foreach (var ch in seq)
        {
            var delta = parser.ProcessChar(ch);
            if (delta != null) deltas.Add(delta.Value);
        }
        Assert.Equal(new[]{-1,1}, deltas.ToArray());
    }
}
