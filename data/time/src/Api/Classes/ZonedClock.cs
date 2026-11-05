namespace time;

/// <summary>Combines a clock with a time zone for current local-time queries.</summary>
public class ZonedClock : IClock, IZonedClock
{
    IClock clock;
    TimeZoneRef zone;

    internal ZonedClock(IClock clock, TimeZoneRef zone)
    {
        TimeValidation.RequireNotNull(clock, "clock");
        TimeValidation.RequireNotNull(zone, "zone");

        this.clock = clock;
        this.zone = zone;
    }

    /// <summary>Creates a zoned view over any clock implementation.</summary>
    /// <param name="clock">The source of current instants.</param>
    /// <param name="zone">The zone used to derive local values.</param>
    /// <returns>A zoned view of the clock.</returns>
    internal static ZonedClock Create(IClock clock, TimeZoneRef zone)
    {
        return new ZonedClock(clock, zone);
    }

    /// <summary>Creates a zoned view of the system clock.</summary><param name="zone">The zone used on every read.</param><returns>A live zoned clock.</returns>
    internal static ZonedClock FromSystem(TimeZoneRef zone)
    {
        return Create(SystemTimeClock.Create(), zone);
    }

    /// <summary>Creates a zoned clock fixed at one instant.</summary><param name="instant">The instant returned on every read.</param><param name="zone">The zone used to derive local values.</param><returns>A deterministic zoned clock.</returns>
    internal static ZonedClock FromFixed(Instant instant, TimeZoneRef zone)
    {
        return Create(FixedTimeClock.Create(instant), zone);
    }

    /// <summary>Gets the zone used by this clock.</summary><returns>The zone reference.</returns>
    public TimeZoneRef GetZone()
    {
        return zone;
    }

    /// <summary>Reads the underlying clock.</summary><returns>The current instant provided by the underlying clock.</returns>
    public Instant GetCurrentInstant()
    {
        return clock.GetCurrentInstant();
    }

    /// <summary>Reads the current instant and converts it through the configured zone.</summary><returns>The current zoned date-time.</returns>
    public ZonedDateTime GetCurrentZonedDateTime()
    {
        return ZonedDateTime.CreateFromInstant(GetCurrentInstant(), zone);
    }

    /// <summary>Gets current local calendar and clock fields in the configured zone.</summary><returns>The current local date-time.</returns>
    public LocalDateTime GetCurrentLocalDateTime()
    {
        return GetCurrentZonedDateTime().GetLocalDateTime();
    }

    /// <summary>Gets the current local date in the configured zone.</summary><returns>Today's local date for this clock and zone.</returns>
    public LocalDate GetCurrentDate()
    {
        return GetCurrentZonedDateTime().GetDate();
    }

    /// <summary>Gets the current local time of day in the configured zone.</summary><returns>The local clock time.</returns>
    public LocalTime GetCurrentTime()
    {
        return GetCurrentZonedDateTime().GetTime();
    }
}
