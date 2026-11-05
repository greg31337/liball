namespace time;

/// <summary>Provides the current instant to code that depends on time.</summary>
/// <remarks>Implement this interface to supply application-specific or deterministic clock behavior.</remarks>
public interface IClock
{
    /// <summary>Reads the current UTC instant.</summary>
    /// <returns>The instant considered current by this clock.</returns>
    Instant GetCurrentInstant();
}
