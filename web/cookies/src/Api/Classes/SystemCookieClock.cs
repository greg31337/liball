namespace cookies;

/// <summary>
/// Supplies current UTC time from the system clock.
/// </summary>
public class SystemCookieClock : ICookieClock
{
    /// <summary>
    /// Creates a system-backed cookie clock.
    /// </summary>
    public SystemCookieClock()
    {
    }

    /// <summary>
    /// Returns the current system time as a UTC instant.
    /// </summary>
    /// <returns>The current UTC instant.</returns>
    public global::System.DateTimeOffset GetUtcNow()
    {
        return global::System.DateTimeOffset.UtcNow;
    }
}
