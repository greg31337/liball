using System.Collections.Generic;

namespace geo;

internal class PackedInterval
{
    internal int Start;
    internal int Count;
}

internal static class IntervalList
{
    internal static PackedInterval[] CreateFixedSizeIntervals(
        int itemCount, int maximumIntervalSize)
    {
        if (itemCount < 0)
        {
            throw new SpatialIndexException(
                "Interval item count cannot be negative.");
        }
        if (maximumIntervalSize < 1)
        {
            throw new SpatialIndexException(
                "Interval size must be positive.");
        }
        List<PackedInterval> intervals = new List<PackedInterval>();
        int start = 0;
        while (start < itemCount)
        {
            int count = maximumIntervalSize;
            if (count > itemCount - start)
            {
                count = itemCount - start;
            }
            PackedInterval interval = new PackedInterval();
            interval.Start = start;
            interval.Count = count;
            intervals.Add(interval);
            start += count;
        }
        return intervals.ToArray();
    }
}
