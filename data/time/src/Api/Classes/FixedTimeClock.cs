namespace time;

/// <summary>Provides a clock that always returns one fixed instant, primarily for deterministic tests.</summary>
public class FixedTimeClock : IClock, IClockZoneProvider
{
    Instant instant;

    internal FixedTimeClock(Instant instant)
    {
        TimeValidation.RequireNotNull(instant, "instant");

        this.instant = instant;
    }

    /// <summary>Creates a clock fixed at the supplied instant.</summary>
    /// <param name="instant">The instant returned by every read.</param>
    /// <returns>A fixed clock.</returns>
    internal static FixedTimeClock Create(Instant instant)
    {
        return new FixedTimeClock(instant);
    }

    /// <summary>Returns the instant captured when this clock was created.</summary>
    /// <returns>The fixed instant.</returns>
    public Instant GetCurrentInstant()
    {
        return instant;
    }

    /// <summary>Combines this clock with a zone for deterministic local-time queries.</summary>
    /// <param name="zone">The zone used to interpret the fixed instant.</param>
    /// <returns>A zoned view of this clock.</returns>
    public ZonedClock InZone(TimeZoneRef zone)
    {
        return ZonedClock.Create(this, zone);
    }
}
