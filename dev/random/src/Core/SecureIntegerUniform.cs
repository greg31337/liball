using System;
using System.Numerics;
using System.Security.Cryptography;

namespace random;

internal static class SecureIntegerUniform
{
    internal static byte NextByteBounded(SecureRandomGenerator generator, byte bound)
    {
        return (byte)NextUInt32(generator, (uint)bound);
    }

    internal static byte NextByteInRange(SecureRandomGenerator generator, byte minimum, byte maximum)
    {
        return (byte)NextUInt32(generator, (uint)minimum, (uint)maximum);
    }

    internal static uint NextUInt32(SecureRandomGenerator generator, uint bound)
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

        uint threshold = IntegerUniform.RejectionThreshold(bound);
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

    internal static uint NextUInt32(SecureRandomGenerator generator, uint minimum, uint maximum)
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

        return minimum + NextUInt32(generator, width);
    }

    internal static int NextInt32(SecureRandomGenerator generator, int bound)
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

    internal static int NextInt32(SecureRandomGenerator generator, int minimum, int maximum)
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
        return (int)((long)minimum + (long)offset);
    }

    internal static ulong NextUInt64(SecureRandomGenerator generator, ulong bound)
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

        ulong threshold = IntegerUniform.RejectionThreshold(bound);
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

    internal static ulong NextUInt64(SecureRandomGenerator generator, ulong minimum, ulong maximum)
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

        return minimum + NextUInt64(generator, width);
    }

    internal static long NextInt64(SecureRandomGenerator generator, long bound)
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

    internal static long NextInt64(SecureRandomGenerator generator, long minimum, long maximum)
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
        return (long)((Int128)minimum + (Int128)offset);
    }

    internal static BigInteger NextBigInteger(SecureRandomGenerator generator, BigInteger bound)
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
                CryptographicOperations.ZeroMemory(bytes);
                return candidate;
            }
        }
    }

    internal static BigInteger NextBigInteger(SecureRandomGenerator generator, BigInteger minimum, BigInteger maximum)
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
}
