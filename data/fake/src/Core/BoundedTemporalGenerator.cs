using System;

namespace fake;

internal static class BoundedTemporalGenerator
{
    internal static DateTime DateTime(
        FakeRandom random,
        DateTime minimum,
        DateTime maximum,
        DateBoundary boundary)
    {
        bool inclusive = Inclusive(boundary);
        if (minimum.Kind != maximum.Kind)
        {
            throw new ArgumentException("maximum must have the same DateTime kind as minimum.", "maximum");
        }
        ValidateRange(minimum.Ticks, maximum.Ticks, inclusive);
        if (inclusive && minimum.Ticks == maximum.Ticks)
        {
            return minimum;
        }
        long ticks = random.LongFromMinimumAndMaximumAndMaximumInclusive(minimum.Ticks, maximum.Ticks, inclusive);
        return new DateTime(ticks, minimum.Kind);
    }

    internal static DateTimeOffset DateTimeOffset(
        FakeRandom random,
        DateTimeOffset minimum,
        DateTimeOffset maximum,
        DateBoundary boundary)
    {
        bool inclusive = Inclusive(boundary);
        long minimumTicks = minimum.UtcTicks;
        long maximumTicks = maximum.UtcTicks;
        ValidateRange(minimumTicks, maximumTicks, inclusive);
        if (inclusive && minimumTicks == maximumTicks)
        {
            return minimum;
        }
        long ticks = random.LongFromMinimumAndMaximumAndMaximumInclusive(minimumTicks, maximumTicks, inclusive);
        return new DateTimeOffset(ticks, System.TimeSpan.Zero);
    }

    internal static void ValidateDateTimeOffsetRange(
        DateTimeOffset minimum,
        DateTimeOffset maximum,
        DateBoundary boundary)
    {
        bool inclusive = Inclusive(boundary);
        ValidateRange(minimum.UtcTicks, maximum.UtcTicks, inclusive);
    }

    internal static DateOnly DateOnly(
        FakeRandom random,
        DateOnly minimum,
        DateOnly maximum,
        DateBoundary boundary)
    {
        bool inclusive = Inclusive(boundary);
        int minimumDay = minimum.DayNumber;
        int maximumDay = maximum.DayNumber;
        ValidateRange(minimumDay, maximumDay, inclusive);
        if (inclusive && minimumDay == maximumDay)
        {
            return minimum;
        }
        int day = random.IntFromMinimumAndMaximumAndMaximumInclusive(minimumDay, maximumDay, inclusive);
        return System.DateOnly.FromDayNumber(day);
    }

    internal static void ValidateDateOnlyRange(
        DateOnly minimum,
        DateOnly maximum,
        DateBoundary boundary)
    {
        bool inclusive = Inclusive(boundary);
        ValidateRange(minimum.DayNumber, maximum.DayNumber, inclusive);
    }

    internal static TimeOnly TimeOnly(
        FakeRandom random,
        TimeOnly minimum,
        TimeOnly maximum,
        DateBoundary boundary)
    {
        bool inclusive = Inclusive(boundary);
        long minimumTicks = minimum.Ticks;
        long maximumTicks = maximum.Ticks;
        ValidateRange(minimumTicks, maximumTicks, inclusive);
        if (inclusive && minimumTicks == maximumTicks)
        {
            return minimum;
        }
        long ticks = random.LongFromMinimumAndMaximumAndMaximumInclusive(minimumTicks, maximumTicks, inclusive);
        return new TimeOnly(ticks);
    }

    internal static TimeSpan TimeSpan(
        FakeRandom random,
        TimeSpan minimum,
        TimeSpan maximum,
        DateBoundary boundary)
    {
        bool inclusive = Inclusive(boundary);
        long minimumTicks = minimum.Ticks;
        long maximumTicks = maximum.Ticks;
        ValidateRange(minimumTicks, maximumTicks, inclusive);
        if (inclusive && minimumTicks == maximumTicks)
        {
            return minimum;
        }
        long ticks = random.LongFromMinimumAndMaximumAndMaximumInclusive(minimumTicks, maximumTicks, inclusive);
        return new System.TimeSpan(ticks);
    }

    static bool Inclusive(DateBoundary boundary)
    {
        if (boundary == DateBoundary.Exclusive)
        {
            return false;
        }
        if (boundary == DateBoundary.Inclusive)
        {
            return true;
        }
        throw new ArgumentOutOfRangeException(
            "boundary",
            boundary,
            "boundary must be Exclusive or Inclusive.");
    }

    static void ValidateRange(long minimum, long maximum, bool inclusive)
    {
        if (minimum > maximum || !inclusive && minimum == maximum)
        {
            throw new ArgumentOutOfRangeException(
                "maximum",
                maximum,
                "maximum must be greater than minimum unless an inclusive singleton interval is requested.");
        }
    }
}
