namespace unicode;

internal class UnicodeRangeValue
{
    internal int Start;
    internal int End;
    internal int Value;

    internal UnicodeRangeValue(int start, int end, int value)
    {
        if (end < start)
        {
            throw new System.ArgumentException("Range end must be greater than or equal to start.");
        }

        Start = start;
        End = end;
        Value = value;
    }

    internal bool Contains(int codePoint)
    {
        if (codePoint < Start)
        {
            return false;
        }

        return codePoint <= End;
    }

    internal int Count()
    {
        return End - Start + 1;
    }

    internal static UnicodeRangeValue[] FromArrays(int[] starts, int[] ends, int[] values)
    {
        System.ArgumentNullException.ThrowIfNull(starts);
        System.ArgumentNullException.ThrowIfNull(ends);
        System.ArgumentNullException.ThrowIfNull(values);

        if (starts.Length != ends.Length)
        {
            throw new System.ArgumentException("Range value arrays must have the same length.");
        }

        if (starts.Length != values.Length)
        {
            throw new System.ArgumentException("Range value arrays must have the same length.");
        }

        UnicodeRangeValue[] ranges = new UnicodeRangeValue[starts.Length];
        int index = 0;
        while (index < starts.Length)
        {
            ranges[index] = new UnicodeRangeValue(starts[index], ends[index], values[index]);
            index = index + 1;
        }

        return ranges;
    }
}
