namespace http;

internal static class SaturatingArithmetic
{
    internal static long Add(long left, long right)
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

    internal static long Subtract(long left, long right)
    {
        if (right > 0 && left < long.MinValue + right)
        {
            return long.MinValue;
        }

        if (right < 0 && left > long.MaxValue + right)
        {
            return long.MaxValue;
        }

        return left - right;
    }

    internal static long Multiply(long left, long right)
    {
        if (left == 0 || right == 0)
        {
            return 0;
        }

        if (left == long.MinValue && right == -1 || right == long.MinValue && left == -1)
        {
            return long.MaxValue;
        }

        if (left > 0)
        {
            if (right > 0 && left > long.MaxValue / right)
            {
                return long.MaxValue;
            }

            if (right < 0 && right < long.MinValue / left)
            {
                return long.MinValue;
            }
        }
        else
        {
            if (right > 0 && left < long.MinValue / right)
            {
                return long.MinValue;
            }

            if (right < 0 && left < long.MaxValue / right)
            {
                return long.MaxValue;
            }
        }

        return left * right;
    }
}
