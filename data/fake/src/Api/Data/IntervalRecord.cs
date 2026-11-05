namespace fake;

/// <summary>Represents an inclusive ordered interval between two instants.</summary>
public class IntervalRecord
{
    /// <summary>The earlier interval instant.</summary>
    public System.DateTimeOffset Start;

    /// <summary>The later interval instant.</summary>
    public System.DateTimeOffset End;

    internal static IntervalRecord FromDateInterval(DateInterval source)
    {
        IntervalRecord value = new IntervalRecord();
        value.Start = source.Start;
        value.End = source.End;
        return value;
    }
}
