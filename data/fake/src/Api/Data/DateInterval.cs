using System;

namespace fake;

/// <summary>
/// Describes an inclusive ordered interval between two instants.
/// </summary>
public class DateInterval
{
    /// <summary>
    /// The earlier interval instant.
    /// </summary>
    public DateTimeOffset Start;

    /// <summary>
    /// The later interval instant.
    /// </summary>
    public DateTimeOffset End;

    internal DateInterval(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }
}
