using CodexCli.Util;
using Xunit;
using System.Collections.Generic;

public class SequenceEqualityComparerTests
{
    [Fact]
    public void HashSetDetectsEquivalentLists()
    {
        var set = new HashSet<IReadOnlyList<string>>(new SequenceEqualityComparer<string>());
        set.Add(new List<string>{"ls","-l"});
        Assert.Contains(new List<string>{"ls","-l"}, set, new SequenceEqualityComparer<string>());
    }
}
