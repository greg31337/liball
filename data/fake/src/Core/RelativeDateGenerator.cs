using System;

namespace fake;

internal static class RelativeDateGenerator
{
    internal static DateTimeOffset Past(
        FakeRandom random,
        DateTimeOffset reference,
        TimeSpan maximumAge)
    {
        Positive(maximumAge, "maximumAge");
        long referenceTicks = reference.UtcTicks;
        if (referenceTicks == DateTimeOffset.MinValue.UtcTicks)
        {
            throw FakeException.Create("Past date generation has no representable instant before the captured reference time.");
        }
        long minimumTicks = referenceTicks - maximumAge.Ticks;
        if (minimumTicks < DateTimeOffset.MinValue.UtcTicks)
        {
            minimumTicks = DateTimeOffset.MinValue.UtcTicks;
        }
        long ticks = random.LongFromMinimumAndMaximum(minimumTicks, referenceTicks);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    internal static DateTimeOffset Future(
        FakeRandom random,
        DateTimeOffset reference,
        TimeSpan maximumLead)
    {
        Positive(maximumLead, "maximumLead");
        long referenceTicks = reference.UtcTicks;
        long maximumRuntimeTicks = DateTimeOffset.MaxValue.UtcTicks;
        if (referenceTicks == maximumRuntimeTicks)
        {
            throw FakeException.Create("Future date generation has no representable instant after the captured reference time.");
        }
        long available = maximumRuntimeTicks - referenceTicks;
        long maximumTicks = maximumLead.Ticks > available
            ? maximumRuntimeTicks
            : referenceTicks + maximumLead.Ticks;
        long ticks = random.LongFromMinimumAndMaximumAndMaximumInclusive(referenceTicks + 1L, maximumTicks, true);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    internal static DateTimeOffset Recent(
        FakeRandom random,
        DateTimeOffset reference,
        TimeSpan window)
    {
        Positive(window, "window");
        long referenceTicks = reference.UtcTicks;
        long minimumTicks = referenceTicks - window.Ticks;
        if (minimumTicks < DateTimeOffset.MinValue.UtcTicks)
        {
            minimumTicks = DateTimeOffset.MinValue.UtcTicks;
        }
        if (minimumTicks == referenceTicks)
        {
            return reference;
        }
        long ticks = random.LongFromMinimumAndMaximumAndMaximumInclusive(minimumTicks, referenceTicks, true);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    internal static DateTimeOffset Soon(
        FakeRandom random,
        DateTimeOffset reference,
        TimeSpan window)
    {
        Positive(window, "window");
        long referenceTicks = reference.UtcTicks;
        long maximumRuntimeTicks = DateTimeOffset.MaxValue.UtcTicks;
        if (referenceTicks == maximumRuntimeTicks)
        {
            throw FakeException.Create("Soon date generation has no representable instant after the captured reference time.");
        }
        long available = maximumRuntimeTicks - referenceTicks;
        long maximumTicks = window.Ticks > available
            ? maximumRuntimeTicks
            : referenceTicks + window.Ticks;
        long ticks = random.LongFromMinimumAndMaximumAndMaximumInclusive(referenceTicks + 1L, maximumTicks, true);
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    internal static DateTimeOffset[] Betweens(
        FakeRandom random,
        DateTimeOffset minimum,
        DateTimeOffset maximum,
        int count,
        DateBoundary boundary,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(count, maximumCount, "count");
        Guard.OutputSize(count, maximumOutputSize, "count");
        BoundedTemporalGenerator.ValidateDateTimeOffsetRange(
            minimum,
            maximum,
            boundary);
        DateTimeOffset[] result = new DateTimeOffset[count];
        int index = 0;
        while (index < count)
        {
            result[index] = BoundedTemporalGenerator.DateTimeOffset(
                random,
                minimum,
                maximum,
                boundary);
            index++;
        }
        System.Array.Sort(result);
        return result;
    }

    static void Positive(TimeSpan value, string parameterName)
    {
        if (value.Ticks <= 0L)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"{parameterName} must be greater than zero.");
        }
    }
}
