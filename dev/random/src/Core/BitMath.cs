using System;

using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;

namespace random;

internal class BitMath
{
    internal static uint RotateLeft(uint value, int offset)
    {
        return BitOperations.RotateLeft(value, offset);
    }

    internal static ulong RotateLeft(ulong value, int offset)
    {
        return BitOperations.RotateLeft(value, offset);
    }

    internal static uint RotateRight(uint value, int offset)
    {
        return BitOperations.RotateRight(value, offset);
    }

    internal static ulong RotateRight(ulong value, int offset)
    {
        return BitOperations.RotateRight(value, offset);
    }

    internal static int BitCount(uint value)
    {
        return BitOperations.PopCount(value);
    }

    internal static int BitCount(ulong value)
    {
        return BitOperations.PopCount(value);
    }

    internal static int LeadingZeroCount(uint value)
    {
        return BitOperations.LeadingZeroCount(value);
    }

    internal static int LeadingZeroCount(ulong value)
    {
        return BitOperations.LeadingZeroCount(value);
    }

    internal static int TrailingZeroCount(uint value)
    {
        return BitOperations.TrailingZeroCount(value);
    }

    internal static int TrailingZeroCount(ulong value)
    {
        return BitOperations.TrailingZeroCount(value);
    }

    internal static uint LowMask32(int bitCount)
    {
        Guard.Argument(bitCount >= 0 && bitCount <= 32, "bitCount", "The bit count must be between zero and 32 inclusive.");
        if (bitCount == 0)
        {
            return 0U;
        }

        if (bitCount == 32)
        {
            return uint.MaxValue;
        }

        return (1U << bitCount) - 1U;
    }

    internal static ulong LowMask64(int bitCount)
    {
        Guard.Argument(bitCount >= 0 && bitCount <= 64, "bitCount", "The bit count must be between zero and 64 inclusive.");
        if (bitCount == 0)
        {
            return 0UL;
        }

        if (bitCount == 64)
        {
            return ulong.MaxValue;
        }

        return (1UL << bitCount) - 1UL;
    }

    internal static uint KeepLowBits(uint value, int bitCount)
    {
        return value & LowMask32(bitCount);
    }

    internal static ulong KeepLowBits(ulong value, int bitCount)
    {
        return value & LowMask64(bitCount);
    }

    internal static ushort ReverseEndianness(ushort value)
    {
        return BinaryPrimitives.ReverseEndianness(value);
    }

    internal static uint ReverseEndianness(uint value)
    {
        return BinaryPrimitives.ReverseEndianness(value);
    }

    internal static ulong ReverseEndianness(ulong value)
    {
        return BinaryPrimitives.ReverseEndianness(value);
    }

    internal static bool IsLittleEndian()
    {
        return BitConverter.IsLittleEndian;
    }

    internal static bool ConstantTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        return CryptographicOperations.FixedTimeEquals(left, right);
    }
}
