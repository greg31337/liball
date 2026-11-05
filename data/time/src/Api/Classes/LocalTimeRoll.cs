namespace time;

/// <summary>Contains a time of day and the number of calendar days crossed by time-only arithmetic.</summary>
/// <remarks>The time is normalized within one day. Apply the day delta separately when a date is available.</remarks>
public class LocalTimeRoll : ILocalTimeRoll
{
    long ticksSinceMidnight;
    int dayDelta;

    internal LocalTimeRoll(long ticksSinceMidnight, int dayDelta)
    {
        TimeValidation.RequireTicksSinceMidnight(ticksSinceMidnight, "ticksSinceMidnight");

        this.ticksSinceMidnight = ticksSinceMidnight;
        this.dayDelta = dayDelta;
    }

    /// <summary>Creates a normalized time arithmetic result.</summary>
    /// <param name="ticksSinceMidnight">The time of day in 100-nanosecond ticks from midnight.</param>
    /// <param name="dayDelta">The signed number of midnights crossed.</param>
    /// <returns>A time and rollover result.</returns>
    internal static LocalTimeRoll Create(long ticksSinceMidnight, int dayDelta)
    {
        return new LocalTimeRoll(ticksSinceMidnight, dayDelta);
    }

    /// <summary>Gets the normalized time of day as 100-nanosecond ticks since midnight.</summary>
    /// <returns>A value from zero through the final tick of the day.</returns>
    public long GetTicksSinceMidnight()
    {
        return ticksSinceMidnight;
    }

    /// <summary>Gets the signed number of calendar days crossed.</summary>
    /// <returns>The day rollover; zero means the result stayed on the same day.</returns>
    public int GetDayDelta()
    {
        return dayDelta;
    }

    /// <summary>Gets the normalized local time portion.</summary>
    /// <returns>The time of day after discarding complete days.</returns>
    public LocalTime GetTime()
    {
        return LocalTime.FromTicksSinceMidnight(ticksSinceMidnight);
    }
}
