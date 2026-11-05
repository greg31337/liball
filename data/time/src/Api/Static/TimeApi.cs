namespace time;

/// <summary>Provides shared entry points for creating and parsing time API values.</summary>
public static class TimeApi
{
    /// <summary>Creates and parses time, calendar, zone, clock, and recurrence objects.</summary>
    public static readonly ITimeFactory Factory = new TimeFactory();
}
