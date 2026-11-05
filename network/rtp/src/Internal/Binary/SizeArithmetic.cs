using System;

namespace rtp;

internal static class SizeArithmetic
{
    internal static int Add(int left, int right)
    {
        if (left < 0)
        {
            throw new ArgumentOutOfRangeException("left");
        }

        if (right < 0)
        {
            throw new ArgumentOutOfRangeException("right");
        }

        if (left > int.MaxValue - right)
        {
            throw new OverflowException("The combined size exceeds Int32.MaxValue.");
        }

        return left + right;
    }

    internal static int Multiply(int value, int factor)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        if (factor < 0)
        {
            throw new ArgumentOutOfRangeException("factor");
        }

        if (value != 0 && factor > int.MaxValue / value)
        {
            throw new OverflowException("The multiplied size exceeds Int32.MaxValue.");
        }

        return value * factor;
    }

    internal static int GetFourBytePadding(int size)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException("size");
        }

        return (4 - (size & 3)) & 3;
    }

    internal static int AlignToFour(int size)
    {
        int padding = GetFourBytePadding(size);
        return Add(size, padding);
    }
}
