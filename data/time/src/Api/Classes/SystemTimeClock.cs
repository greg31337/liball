namespace time;

/// <summary>Reads the current time from the system UTC clock.</summary>
public class SystemTimeClock : IClock, IClockZoneProvider
{
    internal SystemTimeClock()
    {
    }

    /// <summary>Creates a clock backed by the system clock.</summary>
    /// <returns>A system clock.</returns>
    internal static SystemTimeClock Create()
    {
        return new SystemTimeClock();
    }

    /// <summary>Reads the current UTC instant.</summary>
    /// <returns>The instant observed when the method is called.</returns>
    public Instant GetCurrentInstant()
    {
        return Instant.Now();
    }

    /// <summary>Combines this clock with a zone for local current-time queries.</summary>
    /// <param name="zone">The zone used to interpret each current instant.</param>
    /// <returns>A zoned view of this clock.</returns>
    public ZonedClock InZone(TimeZoneRef zone)
    {
        return ZonedClock.Create(this, zone);
    }
}
