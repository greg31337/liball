namespace cookies;

/// <summary>
/// Supplies deterministic UTC time to cookie operations.
/// </summary>
public interface ICookieClock
{
    /// <summary>
    /// Returns the current UTC instant.
    /// </summary>
    /// <returns>The current UTC instant.</returns>
    global::System.DateTimeOffset GetUtcNow();
}
