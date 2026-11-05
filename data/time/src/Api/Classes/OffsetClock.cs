namespace time;

/// <summary>Provides a clock whose current instant is shifted from another clock by a fixed duration.</summary>
public class OffsetClock : IClock, IClockZoneProvider
{
    IClock clock;
    Duration offset;

    internal OffsetClock(IClock clock, Duration offset)
    {
        TimeValidation.RequireNotNull(clock, "clock");
        TimeValidation.RequireNotNull(offset, "offset");

        this.clock = clock;
        this.offset = offset;
    }

    /// <summary>Creates a clock shifted from another clock by a fixed duration.</summary>
    /// <param name="clock">The source of current instants.</param>
    /// <param name="offset">The signed duration added to every source instant.</param>
    /// <returns>A clock that applies the requested offset on every read.</returns>
    internal static OffsetClock Create(IClock clock, Duration offset)
    {
        return new OffsetClock(clock, offset);
    }

    /// <summary>Reads the underlying clock and adds the configured duration.</summary>
    /// <returns>The shifted current instant.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">The shifted instant is outside the supported range.</exception>
    public Instant GetCurrentInstant()
    {
        return clock.GetCurrentInstant().AddDuration(offset);
    }

    /// <summary>Combines this offset clock with a zone for local current-time queries.</summary>
    /// <param name="zone">The zone used to interpret each shifted instant.</param>
    /// <returns>A zoned view of this clock.</returns>
    public ZonedClock InZone(TimeZoneRef zone)
    {
        return ZonedClock.Create(this, zone);
    }
}
