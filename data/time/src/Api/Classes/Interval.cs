using System;

namespace time;

/// <summary>Represents a half-open range of instants whose start is included and end is excluded.</summary>
/// <remarks>Equal endpoints form an empty interval. The end may not precede the start.</remarks>
public class Interval : IInterval
{
    Instant start;
    Instant end;

    internal Interval(Instant start, Instant end)
    {
        TimeValidation.RequireNotNull(start, "start");
        TimeValidation.RequireNotNull(end, "end");

        if (start.CompareTo(end) > 0)
        {
            throw new ArgumentException("Interval start must not be after end.", "end");
        }

        this.start = start;
        this.end = end;
    }

    /// <summary>Creates a half-open interval from two endpoints.</summary>
    /// <param name="start">The included starting instant.</param>
    /// <param name="end">The excluded ending instant.</param>
    /// <returns>The validated interval.</returns>
    internal static Interval Create(Instant start, Instant end)
    {
        return new Interval(start, end);
    }

    /// <summary>Creates an interval by adding a nonnegative duration to a start.</summary>
    /// <param name="start">The included starting instant.</param>
    /// <param name="duration">The nonnegative length.</param>
    /// <returns>An interval ending at start plus duration.</returns>
    internal static Interval FromStartAndDuration(Instant start, Duration duration)
    {
        TimeValidation.RequireNotNull(start, "start");
        TimeValidation.RequireNotNull(duration, "duration");

        if (duration.GetTicks() < 0L)
        {
            throw new ArgumentOutOfRangeException("duration", "Interval duration must not be negative.");
        }

        return new Interval(start, start.AddDuration(duration));
    }

    /// <summary>Parses an ISO interval written as start/end or start/duration.</summary>
    /// <param name="text">The invariant interval text.</param>
    /// <returns>The parsed half-open interval.</returns>
    /// <exception cref="FormatException">The text does not contain exactly one slash or contains invalid components.</exception>
    internal static Interval Parse(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        int separator = IsoParsing.FindSingleSeparator(text, '/');
        string startText = text.Substring(0, separator);
        string endOrDurationText = text.Substring(separator + 1);
        Instant parsedStart = Instant.Parse(startText);

        if (IsoParsing.StartsWithDurationDesignator(endOrDurationText))
        {
            return FromStartAndDuration(parsedStart, Duration.Parse(endOrDurationText));
        }

        return new Interval(parsedStart, Instant.Parse(endOrDurationText));
    }

    /// <summary>Gets the included endpoint.</summary>
    /// <returns>The start instant.</returns>
    public Instant GetStart()
    {
        return start;
    }

    /// <summary>Gets the excluded endpoint.</summary>
    /// <returns>The end instant.</returns>
    public Instant GetEnd()
    {
        return end;
    }

    /// <summary>Gets the exact distance between the endpoints.</summary>
    /// <returns>A nonnegative duration.</returns>
    public Duration GetDuration()
    {
        return start.DurationUntil(end);
    }

    /// <summary>Reports whether the endpoints are equal.</summary>
    /// <returns><c>true</c> when the interval contains no instants.</returns>
    public bool IsEmpty()
    {
        return start.EqualsValue(end);
    }

    /// <summary>Tests half-open membership.</summary>
    /// <param name="instant">The instant to test.</param>
    /// <returns><c>true</c> when start is less than or equal to the instant and the instant is less than end.</returns>
    public bool ContainsInstant(Instant instant)
    {
        TimeValidation.RequireNotNull(instant, "instant");

        return start.CompareTo(instant) <= 0 && instant.CompareTo(end) < 0;
    }

    /// <summary>Tests whether another interval fits fully within this interval.</summary>
    /// <param name="interval">The interval to test.</param>
    /// <returns><c>true</c> when both endpoints fit within this interval.</returns>
    public bool ContainsInterval(Interval interval)
    {
        TimeValidation.RequireNotNull(interval, "interval");

        return start.CompareTo(interval.GetStart()) <= 0 && interval.GetEnd().CompareTo(end) <= 0;
    }

    /// <summary>Tests whether two nonempty intervals share any instants.</summary>
    /// <param name="interval">The interval to test.</param>
    /// <returns><c>true</c> when their interiors intersect; abutting intervals do not overlap.</returns>
    public bool Overlaps(Interval interval)
    {
        TimeValidation.RequireNotNull(interval, "interval");

        if (IsEmpty() || interval.IsEmpty())
        {
            return false;
        }

        return start.CompareTo(interval.GetEnd()) < 0 && interval.GetStart().CompareTo(end) < 0;
    }

    /// <summary>Tests whether one interval ends exactly where the other starts.</summary>
    /// <param name="interval">The interval to test.</param>
    /// <returns><c>true</c> when the intervals touch at one endpoint without overlapping.</returns>
    public bool Abuts(Interval interval)
    {
        TimeValidation.RequireNotNull(interval, "interval");

        return end.EqualsValue(interval.GetStart()) || interval.GetEnd().EqualsValue(start);
    }

    /// <summary>Computes the shared half-open range.</summary>
    /// <param name="interval">The interval to intersect.</param>
    /// <returns>The shared interval, an empty interval when the inputs only abut, or null when they are separated.</returns>
    public Interval? Intersection(Interval interval)
    {
        TimeValidation.RequireNotNull(interval, "interval");

        Instant intersectionStart = MaxInstant(start, interval.GetStart());
        Instant intersectionEnd = MinInstant(end, interval.GetEnd());
        if (intersectionStart.CompareTo(intersectionEnd) > 0)
        {
            return null;
        }

        return new Interval(intersectionStart, intersectionEnd);
    }

    /// <summary>Combines overlapping or abutting intervals.</summary>
    /// <param name="interval">The interval to combine.</param>
    /// <returns>The smallest containing interval, or null when the intervals are disconnected.</returns>
    public Interval? UnionIfConnected(Interval interval)
    {
        TimeValidation.RequireNotNull(interval, "interval");

        if (start.CompareTo(interval.GetEnd()) > 0 || interval.GetStart().CompareTo(end) > 0)
        {
            return null;
        }

        return new Interval(MinInstant(start, interval.GetStart()), MaxInstant(end, interval.GetEnd()));
    }

    /// <summary>Formats the endpoints as an invariant ISO start/end interval.</summary>
    /// <returns>Two instant strings separated by a slash.</returns>
    public string ToIsoString()
    {
        return start.ToIsoString() + "/" + end.ToIsoString();
    }

    static Instant MinInstant(Instant left, Instant right)
    {
        if (left.CompareTo(right) <= 0)
        {
            return left;
        }

        return right;
    }

    static Instant MaxInstant(Instant left, Instant right)
    {
        if (left.CompareTo(right) >= 0)
        {
            return left;
        }

        return right;
    }
}
