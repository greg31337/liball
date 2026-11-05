namespace time;

/// <summary>Chooses how a local date and time maps into a time zone.</summary>
/// <remarks>Implementations can inspect the zone with <see cref="TimeZoneRef.MapLocal"/> and apply an application-specific policy for gaps and overlaps.</remarks>
public interface IZoneResolver
{
    /// <summary>Resolves a local date and time in the supplied zone.</summary>
    /// <param name="zone">The time zone in which the local value occurs.</param>
    /// <param name="localDateTime">The local date and time to resolve.</param>
    /// <returns>The chosen offset date-time.</returns>
    OffsetDateTime ResolveLocal(TimeZoneRef zone, LocalDateTime localDateTime);
}
