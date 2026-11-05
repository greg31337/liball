namespace gif;

internal static class GifMath
{
    internal static long AddSaturating(long left, long right)
    {
        if (right > 0 && left > long.MaxValue - right)
        {
            return long.MaxValue;
        }

        if (right < 0 && left < long.MinValue - right)
        {
            return long.MinValue;
        }

        return left + right;
    }

    internal static long MultiplySaturating(long value, int positiveMultiplier)
    {
        if (positiveMultiplier <= 0)
        {
            return 0;
        }

        if (value > long.MaxValue / positiveMultiplier)
        {
            return long.MaxValue;
        }

        if (value < long.MinValue / positiveMultiplier)
        {
            return long.MinValue;
        }

        return value * positiveMultiplier;
    }
}
