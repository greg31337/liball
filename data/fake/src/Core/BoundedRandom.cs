using System;
using System.Numerics;

namespace fake;

internal static class BoundedRandom
{
    internal static ulong Below(FakeRandom random, ulong bound)
    {
        if (bound == 0UL)
        {
            throw new ArgumentOutOfRangeException("bound", bound, "bound must be greater than zero.");
        }
        if (bound == 1UL)
        {
            return 0UL;
        }
        ulong threshold = (ulong.MaxValue - bound + 1UL) % bound;
        ulong value = random.ULong();
        while (value < threshold)
        {
            value = random.ULong();
        }
        return value % bound;
    }

    internal static BigInteger Below(FakeRandom random, BigInteger bound)
    {
        return Below(random, bound, true);
    }

    internal static BigInteger Below(FakeRandom random, BigInteger bound, bool enforceOutputLimit)
    {
        if (bound <= BigInteger.Zero)
        {
            throw new ArgumentOutOfRangeException("bound", bound, "bound must be greater than zero.");
        }
        if (bound == BigInteger.One)
        {
            return BigInteger.Zero;
        }
        BigInteger maximumCandidate = bound - BigInteger.One;
        int byteCount = maximumCandidate.GetByteCount(true);
        if (enforceOutputLimit)
        {
            Guard.OutputSize(byteCount, random.Context.CapturedOptions.MaximumOutputSize, "maximum");
        }
        long bitLength = maximumCandidate.GetBitLength();
        int excessBits = (int)(((long)byteCount * 8L) - bitLength);
        byte mask = (byte)(255 >> excessBits);
        byte[] bytes = new byte[byteCount];
        BigInteger candidate = bound;
        while (candidate >= bound)
        {
            random.FillEntropy(new Span<byte>(bytes));
            bytes[byteCount - 1] = (byte)(bytes[byteCount - 1] & mask);
            candidate = new BigInteger(bytes, true, false);
        }
        return candidate;
    }

    internal static ulong Ordered(long value)
    {
        if (value < 0L)
        {
            return (ulong)(value - long.MinValue);
        }
        return 9223372036854775808UL + (ulong)value;
    }

    internal static long Signed(ulong value)
    {
        if (value <= (ulong)long.MaxValue)
        {
            return long.MinValue + (long)value;
        }
        return (long)(value - 9223372036854775808UL);
    }
}
