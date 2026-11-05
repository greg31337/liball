namespace cache;

/// <summary>
/// Supplies tick values used for cache expiration and timestamps.
/// </summary>
/// <remarks>
/// Implement this contract to integrate an application-owned time source or a deterministic test clock. Implementations shared across threads must make <see cref="GetUtcTicks"/> thread-safe.
/// </remarks>
public interface ICacheClock
{
    /// <summary>Reads the current UTC-compatible tick value.</summary>
    /// <returns>The tick value to use for cache timing.</returns>
    long GetUtcTicks();
}
