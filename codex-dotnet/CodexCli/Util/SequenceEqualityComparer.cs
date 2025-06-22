using System.Collections.Generic;
using System.Linq;

namespace CodexCli.Util;

/// <summary>
/// Helper comparer to compare sequences by value.
/// Mirrors Hash implementation for Vec<T> in Rust when storing
/// approved commands in ExecCommand safety checks.
/// </summary>
public class SequenceEqualityComparer<T> : IEqualityComparer<IReadOnlyList<T>>
{
    public bool Equals(IReadOnlyList<T>? x, IReadOnlyList<T>? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null || x.Count != y.Count) return false;
        return x.SequenceEqual(y);
    }

    public int GetHashCode(IReadOnlyList<T> obj)
    {
        unchecked
        {
            int hash = 17;
            foreach (var item in obj)
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            return hash;
        }
    }
}
