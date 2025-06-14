namespace CodexCli.Util;

/// <summary>
/// Helper functions for formatting elapsed durations in a human readable form.
/// Mirrors the behavior of the Rust implementation.
/// </summary>
public static class Elapsed
{
    /// <summary>
    /// Format the time elapsed since <paramref name="start"/>.
    /// </summary>
    public static string FormatElapsed(DateTime start)
    {
        return FormatDuration(DateTime.UtcNow - start);
    }

    /// <summary>
    /// Convert a <see cref="TimeSpan"/> into a compact string like
    /// "250ms", "1.50s" or "1m15s".
    /// </summary>
    public static string FormatDuration(TimeSpan duration)
    {
        var millis = (long)duration.TotalMilliseconds;
        if (millis < 1000)
        {
            return $"{millis}ms";
        }
        else if (millis < 60_000)
        {
            return $"{duration.TotalSeconds:F2}s";
        }
        else
        {
            var minutes = (int)duration.TotalMinutes;
            var seconds = duration.Seconds;
            return $"{minutes}m{seconds:00}s";
        }
    }

    /// <summary>
    /// Returns a timestamp string formatted as [YYYY-MM-DDTHH:MM:SS].
    /// </summary>
    public static string Timestamp()
    {
        return $"[{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss}]";
    }
}

