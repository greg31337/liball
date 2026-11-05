namespace unicode;

internal static class UnicodeTableLookup
{
    internal static int LookupRangeValue(UnicodeRangeValue[] ranges, int codePoint, int defaultValue)
    {
        int index = FindRangeValueIndex(ranges, codePoint);

        if (index < 0)
        {
            return defaultValue;
        }

        return ranges[index].Value;
    }

    internal static int FindRangeValueIndex(UnicodeRangeValue[] ranges, int codePoint)
    {
        System.ArgumentNullException.ThrowIfNull(ranges);

        int low = 0;
        int high = ranges.Length - 1;

        while (low <= high)
        {
            int mid = low + ((high - low) / 2);
            UnicodeRangeValue range = ranges[mid];

            if (codePoint < range.Start)
            {
                high = mid - 1;
                continue;
            }

            if (codePoint > range.End)
            {
                low = mid + 1;
                continue;
            }

            return mid;
        }

        return -1;
    }

    internal static bool AreRangeValuesSortedAndNonOverlapping(UnicodeRangeValue[] ranges)
    {
        System.ArgumentNullException.ThrowIfNull(ranges);

        int index = 1;

        while (index < ranges.Length)
        {
            if (ranges[index].Start <= ranges[index - 1].End)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int LookupTwoStageValue(
        int codePoint,
        int[] stage1,
        int[] stage2,
        int blockShift,
        int defaultValue)
    {
        System.ArgumentNullException.ThrowIfNull(stage1);
        System.ArgumentNullException.ThrowIfNull(stage2);

        if (blockShift < 0)
        {
            throw new System.ArgumentOutOfRangeException("blockShift");
        }

        if (blockShift > 20)
        {
            throw new System.ArgumentOutOfRangeException("blockShift");
        }

        if (!UnicodeScalar.Instance.IsValid(codePoint))
        {
            return defaultValue;
        }

        int block = codePoint >> blockShift;
        if (block >= stage1.Length)
        {
            return defaultValue;
        }

        int offset = stage1[block];
        if (offset < 0)
        {
            return defaultValue;
        }

        int lowMask = (1 << blockShift) - 1;
        int low = codePoint & lowMask;
        if (offset >= stage2.Length)
        {
            return defaultValue;
        }

        if (low >= stage2.Length - offset)
        {
            return defaultValue;
        }

        int index = offset + low;
        return stage2[index];
    }
}
