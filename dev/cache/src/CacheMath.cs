using System;

namespace cache;

internal static class CacheMath
{
    internal static long Add(long left, long right)
    {
        if (right > 0L && left > long.MaxValue - right)
        {
            return long.MaxValue;
        }

        if (right < 0L && left < long.MinValue - right)
        {
            return long.MinValue;
        }

        return left + right;
    }

    internal static long Subtract(long left, long right)
    {
        if (right > 0L && left < long.MinValue + right)
        {
            return long.MinValue;
        }

        if (right < 0L && left > long.MaxValue + right)
        {
            return long.MaxValue;
        }

        return left - right;
    }

    internal static long AddScaled(long start, long value, long multiplier)
    {
        if (multiplier <= 0L)
        {
            throw new ArgumentOutOfRangeException("multiplier");
        }

        Int128 result = (Int128)start + (Int128)value * (Int128)multiplier;

        if (result > long.MaxValue)
        {
            return long.MaxValue;
        }

        if (result < long.MinValue)
        {
            return long.MinValue;
        }

        return (long)result;
    }
}
