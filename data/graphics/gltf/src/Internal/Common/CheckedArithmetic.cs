using System;

namespace gltf;

internal static class CheckedArithmetic
{
    internal static int AddInt32(int left, int right)
    {
        long result = (long)left + (long)right;
        if (result < int.MinValue || result > int.MaxValue)
        {
            throw new OverflowException("The Int32 addition overflowed.");
        }

        return (int)result;
    }

    internal static int MultiplyInt32(int left, int right)
    {
        long result = (long)left * (long)right;
        if (result < int.MinValue || result > int.MaxValue)
        {
            throw new OverflowException("The Int32 multiplication overflowed.");
        }

        return (int)result;
    }

    internal static int AddNonNegativeInt32(int left, int right)
    {
        if (left < 0)
        {
            throw new ArgumentOutOfRangeException("left");
        }

        if (right < 0)
        {
            throw new ArgumentOutOfRangeException("right");
        }

        return AddInt32(left, right);
    }

    internal static int MultiplyNonNegativeInt32(int left, int right)
    {
        if (left < 0)
        {
            throw new ArgumentOutOfRangeException("left");
        }

        if (right < 0)
        {
            throw new ArgumentOutOfRangeException("right");
        }

        return MultiplyInt32(left, right);
    }

    internal static int ConvertUInt32ToInt32(uint value)
    {
        if (value > (uint)int.MaxValue)
        {
            throw new OverflowException("The UInt32 value cannot be represented as an Int32.");
        }

        return (int)value;
    }
}
