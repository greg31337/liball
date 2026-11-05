using System;
using System.Numerics;

namespace random;

internal static class IntegerUniform
{
    internal const ulong UInt32DomainSize = 0x100000000UL;

    internal static byte NextByteBounded(IRandomSource generator, byte bound)
    {
        return (byte)NextUInt32(generator, (uint)bound);
    }

    internal static byte NextByteInRange(IRandomSource generator, byte minimum, byte maximum)
    {
        return (byte)NextUInt32(generator, (uint)minimum, (uint)maximum);
    }

    internal static uint NextUInt32(IRandomSource generator, uint bound)
    {
        Guard.NotNull(generator, "generator");
        if (bound == 0U)
        {
            throw RandomArgumentException.Create("bound", "The exclusive upper bound must be positive.");
        }

        if (bound == 1U)
        {
            return 0U;
        }

        uint threshold = RejectionThreshold(bound);
        while (true)
        {
            uint sample = generator.NextUInt32();
            ulong product = (ulong)sample * (ulong)bound;
            uint low = (uint)(product & uint.MaxValue);
            if (low >= threshold)
            {
                return (uint)(product >> 32);
            }
        }
    }

    internal static uint NextUInt32(IRandomSource generator, uint minimum, uint maximum)
    {
        Guard.NotNull(generator, "generator");
        if (minimum >= maximum)
        {
            throw RandomArgumentException.Create("maximum", "The exclusive maximum must be greater than the minimum.");
        }

        uint width = maximum - minimum;
        if (width == 1U)
        {
            return minimum;
        }

        uint offset = NextUInt32(generator, width);
        return minimum + offset;
    }

    internal static int NextInt32(IRandomSource generator, int bound)
    {
        Guard.NotNull(generator, "generator");
        if (bound <= 0)
        {
            throw RandomArgumentException.Create("bound", "The exclusive upper bound must be positive.");
        }

        if (bound == 1)
        {
            return 0;
        }

        return (int)NextUInt32(generator, (uint)bound);
    }

    internal static int NextInt32(IRandomSource generator, int minimum, int maximum)
    {
        Guard.NotNull(generator, "generator");
        if (minimum >= maximum)
        {
            throw RandomArgumentException.Create("maximum", "The exclusive maximum must be greater than the minimum.");
        }

        ulong widthValue = (ulong)((long)maximum - (long)minimum);
        if (widthValue == 1UL)
        {
            return minimum;
        }

        uint offset = NextUInt32(generator, (uint)widthValue);
        long result = (long)minimum + (long)offset;
        return (int)result;
    }

    internal static ulong NextUInt64(IRandomSource generator, ulong bound)
    {
        Guard.NotNull(generator, "generator");
        if (bound == 0UL)
        {
            throw RandomArgumentException.Create("bound", "The exclusive upper bound must be positive.");
        }

        if (bound == 1UL)
        {
            return 0UL;
        }

        ulong threshold = RejectionThreshold(bound);
        while (true)
        {
            ulong sample = generator.NextUInt64();
            UInt128 product = (UInt128)sample * (UInt128)bound;
            ulong low = WideMath.Low64(product);
            if (low >= threshold)
            {
                return WideMath.High64(product);
            }
        }
    }

    internal static ulong NextUInt64(IRandomSource generator, ulong minimum, ulong maximum)
    {
        Guard.NotNull(generator, "generator");
        if (minimum >= maximum)
        {
            throw RandomArgumentException.Create("maximum", "The exclusive maximum must be greater than the minimum.");
        }

        ulong width = maximum - minimum;
        if (width == 1UL)
        {
            return minimum;
        }

        ulong offset = NextUInt64(generator, width);
        return minimum + offset;
    }

    internal static long NextInt64(IRandomSource generator, long bound)
    {
        Guard.NotNull(generator, "generator");
        if (bound <= 0L)
        {
            throw RandomArgumentException.Create("bound", "The exclusive upper bound must be positive.");
        }

        if (bound == 1L)
        {
            return 0L;
        }

        return (long)NextUInt64(generator, (ulong)bound);
    }

    internal static long NextInt64(IRandomSource generator, long minimum, long maximum)
    {
        Guard.NotNull(generator, "generator");
        if (minimum >= maximum)
        {
            throw RandomArgumentException.Create("maximum", "The exclusive maximum must be greater than the minimum.");
        }

        UInt128 widthValue = (UInt128)((Int128)maximum - (Int128)minimum);
        if (widthValue == (UInt128)1)
        {
            return minimum;
        }

        ulong offset = NextUInt64(generator, (ulong)widthValue);
        Int128 result = (Int128)minimum + (Int128)offset;
        return (long)result;
    }

    internal static BigInteger NextBigInteger(IRandomSource generator, BigInteger bound)
    {
        Guard.NotNull(generator, "generator");
        if (bound <= BigInteger.Zero)
        {
            throw RandomArgumentException.Create("bound", "The exclusive upper bound must be positive.");
        }

        if (bound == BigInteger.One)
        {
            return BigInteger.Zero;
        }

        BigInteger maximumCandidate = bound - BigInteger.One;
        int byteCount = maximumCandidate.GetByteCount(true);
        long bitLength = maximumCandidate.GetBitLength();
        int highBitCount = (int)(bitLength % 8L);
        byte highMask = byte.MaxValue;
        if (highBitCount != 0)
        {
            highMask = (byte)((1 << highBitCount) - 1);
        }

        byte[] bytes = new byte[byteCount];
        while (true)
        {
            generator.FillBytes(bytes.AsSpan());
            bytes[byteCount - 1] &= highMask;
            BigInteger candidate = new BigInteger(bytes, true, false);
            if (candidate < bound)
            {
                return candidate;
            }
        }
    }

    internal static BigInteger NextBigInteger(IRandomSource generator, BigInteger minimum, BigInteger maximum)
    {
        Guard.NotNull(generator, "generator");
        if (minimum >= maximum)
        {
            throw RandomArgumentException.Create("maximum", "The exclusive maximum must be greater than the minimum.");
        }

        BigInteger width = maximum - minimum;
        if (width == BigInteger.One)
        {
            return minimum;
        }

        return minimum + NextBigInteger(generator, width);
    }

    internal static uint RejectionThreshold(uint bound)
    {
        if (bound == 0U)
        {
            throw RandomArgumentException.Create("bound", "The exclusive upper bound must be positive.");
        }

        return (uint)(UInt32DomainSize % (ulong)bound);
    }

    internal static ulong RejectionThreshold(ulong bound)
    {
        if (bound == 0UL)
        {
            throw RandomArgumentException.Create("bound", "The exclusive upper bound must be positive.");
        }

        UInt128 domainSize = (UInt128)ulong.MaxValue + (UInt128)1;
        return (ulong)(domainSize % (UInt128)bound);
    }
}
