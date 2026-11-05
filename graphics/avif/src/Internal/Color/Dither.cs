namespace avif;

internal static class Dither
{
    static readonly int[] orderedThresholds = new int[]
    {
        0, 8, 2, 10,
        12, 4, 14, 6,
        3, 11, 1, 9,
        15, 7, 13, 5
    };

    internal static int QuantizeNearest(
        long numerator,
        int denominator,
        int maximum)
    {
        int value = (int)((numerator + denominator / 2) / denominator);
        return Clamp(value, maximum);
    }

    internal static int QuantizeOrdered(
        long numerator,
        int denominator,
        int maximum,
        int x,
        int y)
    {
        int value = (int)(numerator / denominator);
        long remainder = numerator - (long)value * denominator;
        int matrixIndex = (y & 3) * 4 + (x & 3);
        int threshold = orderedThresholds[matrixIndex];
        if (remainder * 32
            > (long)denominator * (threshold * 2 + 1))
        {
            value++;
        }
        return Clamp(value, maximum);
    }

    internal static int QuantizeErrorDiffusion(
        long numerator,
        long error,
        int denominator,
        int maximum)
    {
        long adjusted = numerator + error;
        long rounded;
        if (adjusted >= 0)
        {
            rounded = (adjusted + denominator / 2) / denominator;
        }
        else
        {
            rounded = -((-adjusted + denominator / 2) / denominator);
        }
        return Clamp((int)rounded, maximum);
    }

    internal static long ScaleError(long error, int numerator)
    {
        long scaled = error * numerator;
        return scaled >= 0
            ? (scaled + 8) / 16
            : -((-scaled + 8) / 16);
    }

    static int Clamp(int value, int maximum)
    {
        if (value < 0)
        {
            return 0;
        }
        if (value > maximum)
        {
            return maximum;
        }
        return value;
    }
}
