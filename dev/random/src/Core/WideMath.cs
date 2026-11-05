using System;

namespace random;

internal class WideMath
{
    internal static UInt128 Multiply64To128(ulong left, ulong right)
    {
        return (UInt128)left * (UInt128)right;
    }

    internal static ulong MultiplyHigh64(ulong left, ulong right)
    {
        UInt128 product = Multiply64To128(left, right);
        return High64(product);
    }

    internal static ulong MultiplyLow64(ulong left, ulong right)
    {
        UInt128 product = Multiply64To128(left, right);
        return Low64(product);
    }

    internal static UInt128 AddWithCarry(ulong left, ulong right, ulong carryIn)
    {
        Guard.Argument(carryIn <= 1UL, "carryIn", "The input carry must be zero or one.");
        return (UInt128)left + (UInt128)right + (UInt128)carryIn;
    }

    internal static ulong AddLow64(ulong left, ulong right, ulong carryIn)
    {
        return Low64(AddWithCarry(left, right, carryIn));
    }

    internal static ulong AddCarry64(ulong left, ulong right, ulong carryIn)
    {
        return High64(AddWithCarry(left, right, carryIn));
    }

    internal static ulong Low64(UInt128 value)
    {
        return (ulong)(value & (UInt128)ulong.MaxValue);
    }

    internal static ulong High64(UInt128 value)
    {
        return (ulong)(value >> 64);
    }

    internal static UInt128 FromParts(ulong high, ulong low)
    {
        return ((UInt128)high << 64) | (UInt128)low;
    }

    internal static ulong AddModulo64(ulong left, ulong right)
    {
        ulong available = ulong.MaxValue - left;
        if (right > available)
        {
            return right - available - 1UL;
        }

        return left + right;
    }

    internal static UInt128 AddModulo128(UInt128 left, UInt128 right)
    {
        UInt128 available = UInt128.MaxValue - left;
        if (right > available)
        {
            return right - available - (UInt128)1;
        }

        return left + right;
    }

    internal static UInt128 MultiplyModulo128(UInt128 left, UInt128 right)
    {
        ulong leftLow = Low64(left);
        ulong leftHigh = High64(left);
        ulong rightLow = Low64(right);
        ulong rightHigh = High64(right);

        UInt128 lowProduct = Multiply64To128(leftLow, rightLow);
        ulong crossLeft = MultiplyLow64(leftLow, rightHigh);
        ulong crossRight = MultiplyLow64(leftHigh, rightLow);
        ulong cross = AddModulo64(crossLeft, crossRight);
        UInt128 shiftedCross = (UInt128)cross << 64;

        return AddModulo128(lowProduct, shiftedCross);
    }

    internal static UInt128 MultiplyAddModulo128(UInt128 value, UInt128 multiplier, UInt128 increment)
    {
        UInt128 product = MultiplyModulo128(value, multiplier);
        return AddModulo128(product, increment);
    }
}
