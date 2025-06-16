using System;
using Xunit;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CrossCliFactAttribute : FactAttribute
{
    public CrossCliFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("ENABLE_CROSS_CLI_TESTS") != "1")
            Skip = "cross CLI tests disabled";
    }
}
