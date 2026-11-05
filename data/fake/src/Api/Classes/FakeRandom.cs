using System;

namespace fake;
/// <summary>
/// Provides low-level deterministic or cryptographic random generation.
/// </summary>
/// <remarks>
/// Deterministic mode uses xoshiro256** with SplitMix64 initialization.
/// String seeds are converted to UTF-8 and string and byte-array seeds use
/// 64-bit FNV-1a. These algorithm choices are part of the reproducibility
/// contract. Cryptographic mode uses the platform
/// <see cref="System.Security.Cryptography.RandomNumberGenerator"/> and is not
/// reproducible or snapshot-capable.
/// <para>
/// Deterministic byte filling writes each 64-bit output least-significant byte
/// first. A partial final word is consumed in full.
/// </para>
/// </remarks>
public class FakeRandom : IRandomSource, IFakeRandom
{
    internal GenerationContext Context;
    StableRandom Engine;
    internal FakeRandom(GenerationContext context, ulong seed)
    {
        Context = context;
        Engine = new StableRandom(seed);
    }

    internal static FakeRandom FromContext(GenerationContext context)
    {
        return new FakeRandom(context, StableSeed.FromLong(context.CapturedOptions.Seed));
    }

    internal static FakeRandom FromSeed(GenerationContext context, ulong seed)
    {
        return new FakeRandom(context, seed);
    }

    /// <summary>
    /// Returns the next unsigned 64-bit value from the full output range.
    /// </summary>
    /// <returns>The next value from the configured random source.</returns>
    public ulong ULong()
    {
        if (Context.CapturedOptions.RandomMode == RandomMode.Cryptographic)
        {
            return CryptoRandom.Next();
        }

        return Engine.Next();
    }

    /// <summary>
    /// Returns an unsigned 64-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maximumExclusive</c> is zero.</exception>
    public ulong ULongFromMaximumExclusive(ulong maximumExclusive)
    {
        if (maximumExclusive == 0UL)
        {
            throw new System.ArgumentOutOfRangeException("maximumExclusive", maximumExclusive, "maximumExclusive must be greater than zero.");
        }

        return UnsignedRange(0UL, maximumExclusive, false);
    }

    /// <summary>
    /// Returns an unsigned 64-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    public ulong ULongFromMinimumAndMaximum(ulong minimum, ulong maximum)
    {
        return UnsignedRange(minimum, maximum, false);
    }

    /// <summary>
    /// Returns an unsigned 64-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    public ulong ULongFromMinimumAndMaximumAndMaximumInclusive(ulong minimum, ulong maximum, bool maximumInclusive)
    {
        return UnsignedRange(minimum, maximum, maximumInclusive);
    }

    /// <summary>
    /// Returns a signed 64-bit value from the full range.
    /// </summary>
    /// <returns>A value from <see cref="long.MinValue"/> through <see cref="long.MaxValue"/>.</returns>
    public long Long()
    {
        return BoundedRandom.Signed(ULong());
    }

    /// <summary>
    /// Returns a nonnegative signed 64-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not positive.</exception>
    public long LongFromMaximumExclusive(long maximumExclusive)
    {
        if (maximumExclusive <= 0L)
        {
            throw new System.ArgumentOutOfRangeException("maximumExclusive", maximumExclusive, "maximumExclusive must be greater than zero.");
        }

        return SignedRange(0L, maximumExclusive, false);
    }

    /// <summary>
    /// Returns a signed 64-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    public long LongFromMinimumAndMaximum(long minimum, long maximum)
    {
        return SignedRange(minimum, maximum, false);
    }

    /// <summary>
    /// Returns a signed 64-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    public long LongFromMinimumAndMaximumAndMaximumInclusive(long minimum, long maximum, bool maximumInclusive)
    {
        return SignedRange(minimum, maximum, maximumInclusive);
    }

    /// <summary>
    /// Returns an unsigned 32-bit value from the full range.
    /// </summary>
    /// <returns>A value from zero through <see cref="uint.MaxValue"/>.</returns>
    public uint UInt()
    {
        return (uint)UnsignedRange(0UL, uint.MaxValue, true);
    }

    /// <summary>
    /// Returns an unsigned 32-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maximumExclusive</c> is zero.</exception>
    public uint UIntFromMaximumExclusive(uint maximumExclusive)
    {
        if (maximumExclusive == 0U)
        {
            throw new System.ArgumentOutOfRangeException("maximumExclusive", maximumExclusive, "maximumExclusive must be greater than zero.");
        }

        return (uint)UnsignedRange(0UL, maximumExclusive, false);
    }

    /// <summary>
    /// Returns an unsigned 32-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    public uint UIntFromMinimumAndMaximum(uint minimum, uint maximum)
    {
        return (uint)UnsignedRange(minimum, maximum, false);
    }

    /// <summary>
    /// Returns an unsigned 32-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    public uint UIntFromMinimumAndMaximumAndMaximumInclusive(uint minimum, uint maximum, bool maximumInclusive)
    {
        return (uint)UnsignedRange(minimum, maximum, maximumInclusive);
    }

    /// <summary>
    /// Returns a signed 32-bit value from the full range.
    /// </summary>
    /// <returns>A value from <see cref="int.MinValue"/> through <see cref="int.MaxValue"/>.</returns>
    public int Int()
    {
        return (int)SignedRange(int.MinValue, int.MaxValue, true);
    }

    /// <summary>
    /// Returns a nonnegative signed 32-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not positive.</exception>
    public int IntFromMaximumExclusive(int maximumExclusive)
    {
        if (maximumExclusive <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumExclusive", maximumExclusive, "maximumExclusive must be greater than zero.");
        }

        return (int)SignedRange(0L, maximumExclusive, false);
    }

    /// <summary>
    /// Returns a signed 32-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    public int IntFromMinimumAndMaximum(int minimum, int maximum)
    {
        return (int)SignedRange(minimum, maximum, false);
    }

    /// <summary>
    /// Returns a signed 32-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    public int IntFromMinimumAndMaximumAndMaximumInclusive(int minimum, int maximum, bool maximumInclusive)
    {
        return (int)SignedRange(minimum, maximum, maximumInclusive);
    }

    /// <summary>
    /// Returns an unsigned 16-bit value from the full range.
    /// </summary>
    /// <returns>A value from zero through <see cref="ushort.MaxValue"/>.</returns>
    public ushort UShort()
    {
        return (ushort)UnsignedRange(0UL, ushort.MaxValue, true);
    }

    /// <summary>
    /// Returns an unsigned 16-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maximumExclusive</c> is zero.</exception>
    public ushort UShortFromMaximumExclusive(ushort maximumExclusive)
    {
        if (maximumExclusive == 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumExclusive", maximumExclusive, "maximumExclusive must be greater than zero.");
        }

        return (ushort)UnsignedRange(0UL, maximumExclusive, false);
    }

    /// <summary>
    /// Returns an unsigned 16-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    public ushort UShortFromMinimumAndMaximum(ushort minimum, ushort maximum)
    {
        return (ushort)UnsignedRange(minimum, maximum, false);
    }

    /// <summary>
    /// Returns an unsigned 16-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    public ushort UShortFromMinimumAndMaximumAndMaximumInclusive(ushort minimum, ushort maximum, bool maximumInclusive)
    {
        return (ushort)UnsignedRange(minimum, maximum, maximumInclusive);
    }

    /// <summary>
    /// Returns a signed 16-bit value from the full range.
    /// </summary>
    /// <returns>A value from <see cref="short.MinValue"/> through <see cref="short.MaxValue"/>.</returns>
    public short Short()
    {
        return (short)SignedRange(short.MinValue, short.MaxValue, true);
    }

    /// <summary>
    /// Returns a nonnegative signed 16-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not positive.</exception>
    public short ShortFromMaximumExclusive(short maximumExclusive)
    {
        if (maximumExclusive <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumExclusive", maximumExclusive, "maximumExclusive must be greater than zero.");
        }

        return (short)SignedRange(0L, maximumExclusive, false);
    }

    /// <summary>
    /// Returns a signed 16-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    public short ShortFromMinimumAndMaximum(short minimum, short maximum)
    {
        return (short)SignedRange(minimum, maximum, false);
    }

    /// <summary>
    /// Returns a signed 16-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    public short ShortFromMinimumAndMaximumAndMaximumInclusive(short minimum, short maximum, bool maximumInclusive)
    {
        return (short)SignedRange(minimum, maximum, maximumInclusive);
    }

    /// <summary>
    /// Returns an unsigned 8-bit value from the full range.
    /// </summary>
    /// <returns>A value from zero through <see cref="byte.MaxValue"/>.</returns>
    public byte Byte()
    {
        return (byte)UnsignedRange(0UL, byte.MaxValue, true);
    }

    /// <summary>
    /// Returns an unsigned 8-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>maximumExclusive</c> is zero.</exception>
    public byte ByteFromMaximumExclusive(byte maximumExclusive)
    {
        if (maximumExclusive == 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumExclusive", maximumExclusive, "maximumExclusive must be greater than zero.");
        }

        return (byte)UnsignedRange(0UL, maximumExclusive, false);
    }

    /// <summary>
    /// Returns an unsigned 8-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    public byte ByteFromMinimumAndMaximum(byte minimum, byte maximum)
    {
        return (byte)UnsignedRange(minimum, maximum, false);
    }

    /// <summary>
    /// Returns an unsigned 8-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    public byte ByteFromMinimumAndMaximumAndMaximumInclusive(byte minimum, byte maximum, bool maximumInclusive)
    {
        return (byte)UnsignedRange(minimum, maximum, maximumInclusive);
    }

    /// <summary>
    /// Returns a signed 8-bit value from the full range.
    /// </summary>
    /// <returns>A value from <see cref="sbyte.MinValue"/> through <see cref="sbyte.MaxValue"/>.</returns>
    public sbyte SByte()
    {
        return (sbyte)SignedRange(sbyte.MinValue, sbyte.MaxValue, true);
    }

    /// <summary>
    /// Returns a nonnegative signed 8-bit value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not positive.</exception>
    public sbyte SByteFromMaximumExclusive(sbyte maximumExclusive)
    {
        if (maximumExclusive <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumExclusive", maximumExclusive, "maximumExclusive must be greater than zero.");
        }

        return (sbyte)SignedRange(0L, maximumExclusive, false);
    }

    /// <summary>
    /// Returns a signed 8-bit value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    public sbyte SByteFromMinimumAndMaximum(sbyte minimum, sbyte maximum)
    {
        return (sbyte)SignedRange(minimum, maximum, false);
    }

    /// <summary>
    /// Returns a signed 8-bit value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    public sbyte SByteFromMinimumAndMaximumAndMaximumInclusive(sbyte minimum, sbyte maximum, bool maximumInclusive)
    {
        return (sbyte)SignedRange(minimum, maximum, maximumInclusive);
    }

    /// <summary>
    /// Returns a nonnegative arbitrary-width integer below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero through one less than the maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not positive or requires more than the configured output limit.</exception>
    public System.Numerics.BigInteger BigInteger(System.Numerics.BigInteger maximumExclusive)
    {
        if (maximumExclusive <= System.Numerics.BigInteger.Zero)
        {
            throw new System.ArgumentOutOfRangeException("maximumExclusive", maximumExclusive, "maximumExclusive must be greater than zero.");
        }

        return BigIntegerRange(System.Numerics.BigInteger.Zero, maximumExclusive, false);
    }

    /// <summary>
    /// Returns an arbitrary-width integer between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty or requires more than the configured output limit.</exception>
    public System.Numerics.BigInteger BigIntegerFromMinimumAndMaximum(System.Numerics.BigInteger minimum, System.Numerics.BigInteger maximum)
    {
        return BigIntegerRange(minimum, maximum, false);
    }

    /// <summary>
    /// Returns an arbitrary-width integer with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty or requires more than the configured output limit.</exception>
    public System.Numerics.BigInteger BigIntegerFromMinimumAndMaximumAndMaximumInclusive(System.Numerics.BigInteger minimum, System.Numerics.BigInteger maximum, bool maximumInclusive)
    {
        return BigIntegerRange(minimum, maximum, maximumInclusive);
    }

    /// <summary>
    /// Returns a finite single-precision value from zero inclusive to one exclusive.
    /// </summary>
    /// <returns>A value in the interval from zero inclusive to one exclusive.</returns>
    public float Float()
    {
        return FloatingRandom.Float(this, 0.0f, 1.0f, false);
    }

    /// <summary>
    /// Returns a nonnegative finite single-precision value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The finite exclusive maximum.</param>
    /// <returns>A value from zero inclusive through the exclusive maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not finite and positive.</exception>
    public float FloatFromMaximumExclusive(float maximumExclusive)
    {
        FloatingRandom.Finite(maximumExclusive, "maximumExclusive");
        if (maximumExclusive <= 0.0f)
        {
            throw new System.ArgumentOutOfRangeException("maximumExclusive", maximumExclusive, "maximumExclusive must be greater than zero.");
        }

        return FloatingRandom.Float(this, 0.0f, maximumExclusive, false);
    }

    /// <summary>
    /// Returns a finite single-precision value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the interval is empty.</exception>
    public float FloatFromMinimumAndMaximum(float minimum, float maximum)
    {
        return FloatingRandom.Float(this, minimum, maximum, false);
    }

    /// <summary>
    /// Returns a finite single-precision value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the selected interval is empty.</exception>
    public float FloatFromMinimumAndMaximumAndMaximumInclusive(float minimum, float maximum, bool maximumInclusive)
    {
        return FloatingRandom.Float(this, minimum, maximum, maximumInclusive);
    }

    /// <summary>
    /// Returns a finite double-precision value from zero inclusive to one exclusive.
    /// </summary>
    /// <returns>A value in the interval from zero inclusive to one exclusive.</returns>
    public double Double()
    {
        return FloatingRandom.Double(this, 0.0, 1.0, false);
    }

    /// <summary>
    /// Returns a nonnegative finite double-precision value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The finite exclusive maximum.</param>
    /// <returns>A value from zero inclusive through the exclusive maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not finite and positive.</exception>
    public double DoubleFromMaximumExclusive(double maximumExclusive)
    {
        FloatingRandom.Finite(maximumExclusive, "maximumExclusive");
        if (maximumExclusive <= 0.0)
        {
            throw new System.ArgumentOutOfRangeException("maximumExclusive", maximumExclusive, "maximumExclusive must be greater than zero.");
        }

        return FloatingRandom.Double(this, 0.0, maximumExclusive, false);
    }

    /// <summary>
    /// Returns a finite double-precision value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the interval is empty.</exception>
    public double DoubleFromMinimumAndMaximum(double minimum, double maximum)
    {
        return FloatingRandom.Double(this, minimum, maximum, false);
    }

    /// <summary>
    /// Returns a finite double-precision value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the selected interval is empty.</exception>
    public double DoubleFromMinimumAndMaximumAndMaximumInclusive(double minimum, double maximum, bool maximumInclusive)
    {
        return FloatingRandom.Double(this, minimum, maximum, maximumInclusive);
    }

    /// <summary>
    /// Returns a decimal value from zero inclusive to one exclusive.
    /// </summary>
    /// <returns>A value in the interval from zero inclusive to one exclusive.</returns>
    public decimal Decimal()
    {
        return FloatingRandom.Decimal(this, decimal.Zero, decimal.One, false);
    }

    /// <summary>
    /// Returns a nonnegative decimal value below an exclusive maximum.
    /// </summary>
    /// <param name="maximumExclusive">The exclusive maximum.</param>
    /// <returns>A value from zero inclusive through the exclusive maximum.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the maximum is not positive.</exception>
    public decimal DecimalFromMaximumExclusive(decimal maximumExclusive)
    {
        if (maximumExclusive <= decimal.Zero)
        {
            throw new System.ArgumentOutOfRangeException("maximumExclusive", maximumExclusive, "maximumExclusive must be greater than zero.");
        }

        return FloatingRandom.Decimal(this, decimal.Zero, maximumExclusive, false);
    }

    /// <summary>
    /// Returns a decimal value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The exclusive maximum.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the interval is empty.</exception>
    public decimal DecimalFromMinimumAndMaximum(decimal minimum, decimal maximum)
    {
        return FloatingRandom.Decimal(this, minimum, maximum, false);
    }

    /// <summary>
    /// Returns a decimal value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the selected interval is empty.</exception>
    public decimal DecimalFromMinimumAndMaximumAndMaximumInclusive(decimal minimum, decimal maximum, bool maximumInclusive)
    {
        return FloatingRandom.Decimal(this, minimum, maximum, maximumInclusive);
    }

    /// <summary>
    /// Returns an unbiased boolean with equal false and true probabilities.
    /// </summary>
    /// <returns>A random boolean.</returns>
    public bool Bool()
    {
        return ULongFromMaximumExclusive(2UL) == 1UL;
    }

    /// <summary>
    /// Returns a boolean using a probability of true.
    /// </summary>
    /// <param name="probability">The finite probability from zero through one.</param>
    /// <returns>True with the requested probability; otherwise false.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>probability</c> is non-finite or outside zero through one.</exception>
    public bool BoolFromProbability(double probability)
    {
        return Probability(probability);
    }

    /// <summary>
    /// Returns a boolean using an exact integer ratio of true outcomes.
    /// </summary>
    /// <param name="numerator">The number of true outcomes.</param>
    /// <param name="denominator">The total number of outcomes.</param>
    /// <returns>True with probability <c>numerator / denominator</c>; otherwise false.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the denominator is not positive or the numerator is outside zero through the denominator.</exception>
    public bool BoolFromNumeratorAndDenominator(long numerator, long denominator)
    {
        return Ratio(numerator, denominator);
    }

    /// <summary>
    /// Returns a boolean using a probability of true.
    /// </summary>
    /// <remarks>Exact zero and one probabilities do not consume random state.</remarks>
    /// <param name="probability">The finite probability from zero through one.</param>
    /// <returns>True with the requested probability; otherwise false.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>probability</c> is non-finite or outside zero through one.</exception>
    public bool Probability(double probability)
    {
        return ProbabilityRandom.Probability(this, probability);
    }

    /// <summary>
    /// Returns a boolean using an exact integer ratio of true outcomes.
    /// </summary>
    /// <remarks>Zero and full ratios do not consume random state.</remarks>
    /// <param name="numerator">The number of true outcomes.</param>
    /// <param name="denominator">The total number of outcomes.</param>
    /// <returns>True with probability <c>numerator / denominator</c>; otherwise false.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the denominator is not positive or the numerator is outside zero through the denominator.</exception>
    public bool ProbabilityFromNumeratorAndDenominator(long numerator, long denominator)
    {
        return Ratio(numerator, denominator);
    }

    /// <summary>
    /// Returns a boolean using an exact integer ratio of true outcomes.
    /// </summary>
    /// <remarks>Zero and full ratios do not consume random state.</remarks>
    /// <param name="numerator">The number of true outcomes.</param>
    /// <param name="denominator">The total number of outcomes.</param>
    /// <returns>True with probability <c>numerator / denominator</c>; otherwise false.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the denominator is not positive or the numerator is outside zero through the denominator.</exception>
    public bool Ratio(long numerator, long denominator)
    {
        return ProbabilityRandom.Ratio(this, numerator, denominator);
    }

    /// <summary>
    /// Optionally returns a value with equal missing and present probabilities.
    /// </summary>
    /// <param name="value">The value to return when selected.</param>
    /// <returns>The value when selected; otherwise <c>null</c>.</returns>
    public object? Maybe(object? value)
    {
        return Bool() ? value : null;
    }

    /// <summary>
    /// Optionally returns a value using a probability of presence.
    /// </summary>
    /// <param name="value">The value to return when selected.</param>
    /// <param name="probability">The finite probability of returning the value.</param>
    /// <returns>The value when selected; otherwise <c>null</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>probability</c> is non-finite or outside zero through one.</exception>
    public object? MaybeFromValueAndProbability(object? value, double probability)
    {
        return Probability(probability) ? value : null;
    }

    /// <summary>
    /// Optionally returns a value using an exact ratio of present outcomes.
    /// </summary>
    /// <param name="value">The value to return when selected.</param>
    /// <param name="numerator">The number of present outcomes.</param>
    /// <param name="denominator">The total number of outcomes.</param>
    /// <returns>The value when selected; otherwise <c>null</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the denominator is not positive or the numerator is outside zero through the denominator.</exception>
    public object? MaybeFromValueAndNumeratorAndDenominator(object? value, long numerator, long denominator)
    {
        return Ratio(numerator, denominator) ? value : null;
    }

    /// <summary>
    /// Optionally invokes a factory with equal missing and present probabilities.
    /// </summary>
    /// <param name="factory">The factory to invoke only when selected.</param>
    /// <returns>The factory result when selected; otherwise <c>null</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>factory</c> is null.</exception>
    public object? MaybeFromFactory(System.Func<object?> factory)
    {
        Guard.NotNull(factory, "factory");
        return Bool() ? factory() : null;
    }

    /// <summary>
    /// Optionally invokes a factory using a probability of presence.
    /// </summary>
    /// <param name="factory">The factory to invoke only when selected.</param>
    /// <param name="probability">The finite probability of invoking the factory.</param>
    /// <returns>The factory result when selected; otherwise <c>null</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>factory</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>probability</c> is non-finite or outside zero through one.</exception>
    public object? MaybeFromFactoryAndProbability(System.Func<object?> factory, double probability)
    {
        Guard.NotNull(factory, "factory");
        return Probability(probability) ? factory() : null;
    }

    /// <summary>
    /// Optionally invokes a factory using an exact ratio of present outcomes.
    /// </summary>
    /// <param name="factory">The factory to invoke only when selected.</param>
    /// <param name="numerator">The number of present outcomes.</param>
    /// <param name="denominator">The total number of outcomes.</param>
    /// <returns>The factory result when selected; otherwise <c>null</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>factory</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the denominator is not positive or the numerator is outside zero through the denominator.</exception>
    public object? MaybeFromFactoryAndNumeratorAndDenominator(System.Func<object?> factory, long numerator, long denominator)
    {
        Guard.NotNull(factory, "factory");
        return Ratio(numerator, denominator) ? factory() : null;
    }

    /// <summary>
    /// Returns a uniform value from zero inclusive to one exclusive.
    /// </summary>
    /// <returns>A uniform value in the unit interval.</returns>
    public double Uniform()
    {
        return Double();
    }

    /// <summary>
    /// Returns a uniform value between an inclusive minimum and exclusive maximum.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite exclusive maximum.</param>
    /// <returns>A uniform value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the interval is empty.</exception>
    public double UniformFromMinimumAndMaximum(double minimum, double maximum)
    {
        return DoubleFromMinimumAndMaximum(minimum, maximum);
    }

    /// <summary>
    /// Returns a uniform value with an explicit maximum-bound choice.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite maximum.</param>
    /// <param name="maximumInclusive">Whether the maximum is included.</param>
    /// <returns>A uniform value within the requested interval.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the selected interval is empty.</exception>
    public double UniformFromMinimumAndMaximumAndMaximumInclusive(double minimum, double maximum, bool maximumInclusive)
    {
        return DoubleFromMinimumAndMaximumAndMaximumInclusive(minimum, maximum, maximumInclusive);
    }

    /// <summary>
    /// Returns a standard normal value with mean zero and standard deviation one.
    /// </summary>
    /// <returns>A finite normally distributed value.</returns>
    public double Normal()
    {
        return RandomDistributions.Normal(this, 0.0, 1.0);
    }

    /// <summary>
    /// Returns a normally distributed value.
    /// </summary>
    /// <param name="mean">The finite distribution mean.</param>
    /// <param name="standardDeviation">The finite, nonnegative standard deviation.</param>
    /// <returns>A finite normally distributed value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a parameter is non-finite or the standard deviation is negative.</exception>
    public double NormalFromMeanAndStandardDeviation(double mean, double standardDeviation)
    {
        return RandomDistributions.Normal(this, mean, standardDeviation);
    }

    /// <summary>
    /// Returns a standard log-normal value whose logarithm has mean zero and standard deviation one.
    /// </summary>
    /// <returns>A positive finite log-normally distributed value.</returns>
    public double LogNormal()
    {
        return RandomDistributions.LogNormal(this, 0.0, 1.0);
    }

    /// <summary>
    /// Returns a log-normally distributed value.
    /// </summary>
    /// <param name="mean">The finite mean of the underlying normal logarithm.</param>
    /// <param name="standardDeviation">The finite, nonnegative standard deviation of the logarithm.</param>
    /// <returns>A positive finite log-normally distributed value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a parameter is non-finite or the standard deviation is negative.</exception>
    public double LogNormalFromMeanAndStandardDeviation(double mean, double standardDeviation)
    {
        return RandomDistributions.LogNormal(this, mean, standardDeviation);
    }

    /// <summary>
    /// Returns an exponentially distributed value with rate one.
    /// </summary>
    /// <returns>A nonnegative finite exponentially distributed value.</returns>
    public double Exponential()
    {
        return RandomDistributions.Exponential(this, 1.0);
    }

    /// <summary>
    /// Returns an exponentially distributed value with a specified rate.
    /// </summary>
    /// <param name="rate">The finite positive rate.</param>
    /// <returns>A nonnegative finite exponentially distributed value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>rate</c> is non-finite or not positive.</exception>
    public double ExponentialFromRate(double rate)
    {
        return RandomDistributions.Exponential(this, rate);
    }

    /// <summary>
    /// Returns a triangular value from zero through one with mode one half.
    /// </summary>
    /// <returns>A finite triangularly distributed value.</returns>
    public double Triangular()
    {
        return RandomDistributions.Triangular(this, 0.0, 1.0, 0.5);
    }

    /// <summary>
    /// Returns a symmetric triangular value between two bounds.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite exclusive maximum.</param>
    /// <returns>A finite triangularly distributed value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is non-finite or the interval is empty.</exception>
    public double TriangularFromMinimumAndMaximum(double minimum, double maximum)
    {
        double mode = RandomDistributions.Midpoint(minimum, maximum);
        return RandomDistributions.Triangular(this, minimum, maximum, mode);
    }

    /// <summary>
    /// Returns a triangular value between two bounds with a specified mode.
    /// </summary>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite exclusive maximum.</param>
    /// <param name="mode">The finite mode between the bounds, inclusive.</param>
    /// <returns>A finite triangularly distributed value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a value is non-finite, the interval is empty, or the mode is outside it.</exception>
    public double TriangularFromMinimumAndMaximumAndMode(double minimum, double maximum, double mode)
    {
        return RandomDistributions.Triangular(this, minimum, maximum, mode);
    }

    /// <summary>
    /// Returns an index from a weighted discrete distribution.
    /// </summary>
    /// <remarks>
    /// Weights may be normalized or relative. Zero-weight indexes are never selected.
    /// Each positive-weight interval excludes its upper boundary, so an exact cumulative
    /// boundary selects the next positive-weight index.
    /// </remarks>
    /// <param name="weights">The finite, nonnegative weights containing at least one positive value.</param>
    /// <returns>The selected zero-based index.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>weights</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>weights</c> is empty or contains no positive value.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the array exceeds the configured count limit or contains a negative or non-finite weight.</exception>
    public int WeightedDiscrete(double[] weights)
    {
        return RandomDistributions.WeightedDiscrete(this, weights);
    }

    /// <summary>
    /// Selects one string according to corresponding weights.
    /// </summary>
    /// <remarks>
    /// Weights may be normalized or relative. Zero-weight values are never selected.
    /// Each positive-weight interval excludes its upper boundary, so an exact cumulative
    /// boundary selects the next positive-weight value.
    /// </remarks>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="weights">One finite, nonnegative weight per value, with at least one positive weight.</param>
    /// <returns>The selected string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> or <c>weights</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the arrays are empty, their lengths differ, or no weight is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an array exceeds the configured count limit or a weight is negative or non-finite.</exception>
    public string WeightedChoice(string[] values, double[] weights)
    {
        Guard.NotNull(values, "values");
        return values[Selection.WeightedChoiceIndex(this, values.Length, weights)];
    }

    /// <summary>
    /// Selects one object according to corresponding weights.
    /// </summary>
    /// <remarks>
    /// Weights may be normalized or relative. Zero-weight values are never selected.
    /// Each positive-weight interval excludes its upper boundary, so an exact cumulative
    /// boundary selects the next positive-weight value.
    /// </remarks>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="weights">One finite, nonnegative weight per value, with at least one positive weight.</param>
    /// <returns>The selected object.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> or <c>weights</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the arrays are empty, their lengths differ, or no weight is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an array exceeds the configured count limit or a weight is negative or non-finite.</exception>
    public object WeightedChoiceFromValuesAndWeights(object[] values, double[] weights)
    {
        Guard.NotNull(values, "values");
        return values[Selection.WeightedChoiceIndex(this, values.Length, weights)];
    }

    /// <summary>
    /// Selects one string uniformly from an array.
    /// </summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the source exceeds the configured count limit.</exception>
    public string Choice(string[] values)
    {
        Guard.NotNull(values, "values");
        return values[Selection.ChoiceIndex(this, values.Length)];
    }

    /// <summary>
    /// Selects one object uniformly from an array.
    /// </summary>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected object.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the source exceeds the configured count limit.</exception>
    public object ChoiceFromValues(object[] values)
    {
        Guard.NotNull(values, "values");
        return values[Selection.ChoiceIndex(this, values.Length)];
    }

    /// <summary>
    /// Selects strings uniformly with replacement.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of results.</param>
    /// <returns>A new array containing the selected strings.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty and <c>count</c> is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an input exceeds the configured count limit or <c>count</c> is negative.</exception>
    public string[] Choices(string[] values, int count)
    {
        return ChoicesFromValuesAndCountAndWithReplacement(values, count, true);
    }

    /// <summary>
    /// Selects strings uniformly with an explicit replacement choice.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of results.</param>
    /// <param name="withReplacement">Whether a source position may be selected more than once.</param>
    /// <returns>A new array containing the selected strings.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty and <c>count</c> is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an input exceeds the configured count limit, <c>count</c> is negative, or insufficient values exist without replacement.</exception>
    public string[] ChoicesFromValuesAndCountAndWithReplacement(string[] values, int count, bool withReplacement)
    {
        Guard.NotNull(values, "values");
        int[] indexes = Selection.ChoiceIndexes(this, values.Length, count, withReplacement);
        string[] selected = new string[indexes.Length];
        int index = 0;
        while (index < indexes.Length)
        {
            selected[index] = values[indexes[index]];
            index = index + 1;
        }

        return selected;
    }

    /// <summary>
    /// Selects objects uniformly with replacement.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of results.</param>
    /// <returns>A new array containing the selected objects.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty and <c>count</c> is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an input exceeds the configured count limit or <c>count</c> is negative.</exception>
    public object[] ChoicesFromValuesAndCount(object[] values, int count)
    {
        return ChoicesFromValuesAndCountAndWithReplacementUsingObjectArrayAndInt32AndBoolean(values, count, true);
    }

    /// <summary>
    /// Selects objects uniformly with an explicit replacement choice.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of results.</param>
    /// <param name="withReplacement">Whether a source position may be selected more than once.</param>
    /// <returns>A new array containing the selected objects.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty and <c>count</c> is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an input exceeds the configured count limit, <c>count</c> is negative, or insufficient values exist without replacement.</exception>
    public object[] ChoicesFromValuesAndCountAndWithReplacementUsingObjectArrayAndInt32AndBoolean(object[] values, int count, bool withReplacement)
    {
        Guard.NotNull(values, "values");
        int[] indexes = Selection.ChoiceIndexes(this, values.Length, count, withReplacement);
        object[] selected = new object[indexes.Length];
        int index = 0;
        while (index < indexes.Length)
        {
            selected[index] = values[indexes[index]];
            index = index + 1;
        }

        return selected;
    }

    /// <summary>
    /// Samples string source positions uniformly without replacement.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of results.</param>
    /// <returns>A new array containing the sampled strings.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty and <c>count</c> is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an input exceeds the configured count limit, <c>count</c> is negative, or insufficient source positions exist.</exception>
    public string[] Sample(string[] values, int count)
    {
        return ChoicesFromValuesAndCountAndWithReplacement(values, count, false);
    }

    /// <summary>
    /// Samples object source positions uniformly without replacement.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of results.</param>
    /// <returns>A new array containing the sampled objects.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>values</c> is empty and <c>count</c> is positive.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an input exceeds the configured count limit, <c>count</c> is negative, or insufficient source positions exist.</exception>
    public object[] SampleFromValuesAndCount(object[] values, int count)
    {
        return ChoicesFromValuesAndCountAndWithReplacementUsingObjectArrayAndInt32AndBoolean(values, count, false);
    }

    /// <summary>
    /// Returns a uniformly shuffled copy of a string array.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <returns>A new array containing every source position once.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the source exceeds the configured count limit.</exception>
    public string[] Shuffle(string[] values)
    {
        Guard.NotNull(values, "values");
        return ChoicesFromValuesAndCountAndWithReplacement(values, values.Length, false);
    }

    /// <summary>
    /// Returns a uniformly shuffled copy of an object array.
    /// </summary>
    /// <param name="values">The source array.</param>
    /// <returns>A new array containing every source position once.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>values</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the source exceeds the configured count limit.</exception>
    public object[] ShuffleFromValues(object[] values)
    {
        Guard.NotNull(values, "values");
        return ChoicesFromValuesAndCountAndWithReplacementUsingObjectArrayAndInt32AndBoolean(values, values.Length, false);
    }

    /// <summary>
    /// Invokes a callback a bounded number of times and returns the results in invocation order.
    /// </summary>
    /// <param name="count">The number of callback results to create.</param>
    /// <param name="generate">The callback invoked once for each result.</param>
    /// <returns>A new array containing the generated objects.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>generate</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured count limit.</exception>
    public object[] Repeat(int count, Func<object> generate)
    {
        Guard.NotNull(generate, "generate");
        Guard.Count(count, Context.CapturedOptions.MaximumCount, "count");
        object[] result = new object[count];
        FillFromDestinationAndGenerate(result, generate);
        return result;
    }

    /// <summary>
    /// Fills an existing object array by invoking a callback once for each destination position.
    /// </summary>
    /// <remarks>If the callback throws, the completed destination prefix remains filled and the exception is propagated.</remarks>
    /// <param name="destination">The object array to fill in ascending index order.</param>
    /// <param name="generate">The callback invoked once for each destination position.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the destination exceeds the configured count limit.</exception>
    public void FillFromDestinationAndGenerate(object[] destination, Func<object> generate)
    {
        Guard.NotNull(destination, "destination");
        Guard.NotNull(generate, "generate");
        Guard.Count(destination.Length, Context.CapturedOptions.MaximumCount, "destination");
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = generate();
            index = index + 1;
        }
    }

    /// <summary>
    /// Selects one declared value uniformly from a runtime enum type.
    /// </summary>
    /// <param name="enumType">The enum type whose declared values form the selection set.</param>
    /// <returns>The selected boxed enum value.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>enumType</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>enumType</c> does not identify an enum.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the number of declared values exceeds the configured count limit.</exception>
    /// <exception cref="FakeException">Thrown when the enum declares no values.</exception>
    public object EnumValue(Type enumType)
    {
        Guard.NotNull(enumType, "enumType");
        if (!enumType.IsEnum)
        {
            throw new ArgumentException("enumType must identify an enum.", "enumType");
        }

        Array values = Enum.GetValues(enumType);
        Guard.Count(values.Length, Context.CapturedOptions.MaximumCount, "enumType");
        if (values.Length == 0)
        {
            throw FakeException.Create("Enum selection exhausted because enumType declares no values.");
        }

        object? selected = values.GetValue(IntFromMaximumExclusive(values.Length));
        if (selected == null)
        {
            throw FakeException.Create("Enum selection failed to return a declared value.");
        }
        return selected;
    }

    /// <summary>
    /// Creates an unbounded sequence starting at zero and incrementing by one.
    /// </summary>
    /// <returns>A new independent sequence state.</returns>
    public SequenceState Sequence()
    {
        return new SequenceState(0L, 1L, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>
    /// Creates an unbounded arithmetic sequence.
    /// </summary>
    /// <param name="start">The first value.</param>
    /// <param name="increment">The nonzero amount added after each value.</param>
    /// <returns>A new independent sequence state.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>increment</c> is zero.</exception>
    public SequenceState SequenceFromStartAndIncrement(long start, long increment)
    {
        return new SequenceState(start, increment, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>
    /// Creates a finite arithmetic sequence with an exclusive boundary.
    /// </summary>
    /// <param name="start">The first value.</param>
    /// <param name="increment">The nonzero amount added after each value.</param>
    /// <param name="boundaryExclusive">The exclusive boundary in the direction of the increment.</param>
    /// <returns>A new independent sequence state.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the increment is zero or the boundary is not beyond the start in its direction.</exception>
    public SequenceState SequenceFromStartAndIncrementAndBoundaryExclusive(long start, long increment, long boundaryExclusive)
    {
        return SequenceState.Bounded(start, increment, boundaryExclusive, false, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>
    /// Creates a cyclic arithmetic sequence with an exclusive boundary.
    /// </summary>
    /// <remarks>After the last value before the boundary, the next value is the configured start.</remarks>
    /// <param name="start">The first value.</param>
    /// <param name="increment">The nonzero amount added after each value.</param>
    /// <param name="boundaryExclusive">The exclusive boundary in the direction of the increment.</param>
    /// <returns>A new independent cyclic sequence state.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the increment is zero or the boundary is not beyond the start in its direction.</exception>
    public SequenceState Cycle(long start, long increment, long boundaryExclusive)
    {
        return SequenceState.Bounded(start, increment, boundaryExclusive, true, Context.CapturedOptions.MaximumCount);
    }

    /// <summary>Returns a cached named dataset sequence starting at zero and incrementing by one.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <returns>The named sequence shared by providers in this generation context.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="FakeException">Thrown when the name was previously used with different sequence configuration.</exception>
    public SequenceState SequenceFromName(string name)
    {
        return Context.GetSequenceScope(name, 0L, 1L);
    }

    /// <summary>Returns a cached named arithmetic dataset sequence.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <param name="start">The first value returned after creation or reset.</param>
    /// <param name="increment">The nonzero amount added after each value.</param>
    /// <returns>The named sequence shared by providers in this generation context.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>increment</c> is zero.</exception>
    /// <exception cref="FakeException">Thrown when the name was previously used with different sequence configuration.</exception>
    public SequenceState SequenceFromNameAndStartAndIncrement(string name, long start, long increment)
    {
        return Context.GetSequenceScope(name, start, increment);
    }

    /// <summary>Resets a named dataset sequence if it has been created.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public void ResetSequence(string name)
    {
        Context.ResetSequence(name);
    }

    /// <summary>Resets every named dataset sequence in this generation context.</summary>
    public void ResetSequenceFromNoArguments()
    {
        Context.ResetSequenceFromNoArguments();
    }

    /// <summary>
    /// Returns a cached named uniqueness scope shared by this generator's providers and random forks.
    /// </summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <returns>The named uniqueness scope.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public UniqueScope Unique(string name)
    {
        return Context.GetUniqueScope(name);
    }

    /// <summary>
    /// Generates a string unique within a named scope.
    /// </summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <param name="generate">The callback used to generate candidate strings.</param>
    /// <returns>The first candidate not previously accepted by the named scope.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="FakeException">Thrown when the retry budget is exhausted.</exception>
    public string UniqueFromNameAndGenerate(string name, Func<string> generate)
    {
        return Unique(name).Next(generate);
    }

    /// <summary>
    /// Generates an object with a composite string key unique within a named scope.
    /// </summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <param name="generate">The callback used to generate candidate objects.</param>
    /// <param name="compositeKey">The callback that selects the ordered string key parts.</param>
    /// <returns>The first candidate whose composite key was not previously accepted by the named scope.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="FakeException">Thrown when key selection fails or the retry budget is exhausted.</exception>
    public object UniqueFromNameAndGenerateAndCompositeKey(string name, Func<object> generate, Func<object, string[]> compositeKey)
    {
        return Unique(name).NextFromGenerateAndCompositeKey(generate, compositeKey);
    }

    /// <summary>
    /// Resets a named uniqueness scope, creating it if necessary.
    /// </summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public void ResetUnique(string name)
    {
        Context.ResetUnique(name);
    }

    /// <summary>
    /// Resets every uniqueness scope created by this generator's context.
    /// </summary>
    public void ResetUniqueFromNoArguments()
    {
        Context.ResetUniqueFromNoArguments();
    }

    /// <summary>
    /// Creates an array and fills it from the configured random source.
    /// </summary>
    /// <param name="count">The number of bytes to create.</param>
    /// <returns>A newly allocated array containing random bytes.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>count</c> is negative or exceeds the configured output limit.</exception>
    public byte[] Bytes(int count)
    {
        int length = Guard.OutputSize(count, Context.CapturedOptions.MaximumOutputSize, "count");
        byte[] destination = new byte[length];
        Fill(destination);
        return destination;
    }

    /// <summary>
    /// Fills an array from the configured random source.
    /// </summary>
    /// <param name="destination">The array to fill.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>destination</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the destination exceeds the configured output limit.</exception>
    public void Fill(byte[] destination)
    {
        Guard.NotNull(destination, "destination");
        FillFromDestination(new Span<byte>(destination));
    }

    /// <summary>
    /// Fills a span from the configured random source.
    /// </summary>
    /// <param name="destination">The span to fill.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the destination exceeds the configured output limit.</exception>
    public void FillFromDestination(Span<byte> destination)
    {
        Guard.OutputSize(destination.Length, Context.CapturedOptions.MaximumOutputSize, "destination");
        FillEntropy(destination);
    }

    internal void FillEntropy(Span<byte> destination)
    {
        if (Context.CapturedOptions.RandomMode == RandomMode.Cryptographic)
        {
            CryptoRandom.Fill(destination);
            return;
        }

        Engine.Fill(destination);
    }

    /// <summary>
    /// Restarts a deterministic stream from its current seed.
    /// Cryptographic mode continues from the platform random source.
    /// </summary>
    public void Reset()
    {
        Engine.Reset();
    }

    /// <summary>
    /// Captures the exact current position and reset seed of a deterministic stream.
    /// </summary>
    /// <returns>An independent deterministic-state snapshot.</returns>
    /// <exception cref="System.NotSupportedException">Thrown in cryptographic mode.</exception>
    public FakeState Snapshot()
    {
        RequireDeterministicState("Snapshot");
        return Engine.Snapshot();
    }

    /// <summary>
    /// Restores the exact position and reset seed recorded in a deterministic snapshot.
    /// </summary>
    /// <param name="state">The deterministic-state snapshot to restore.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>state</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the snapshot algorithm or state is invalid.</exception>
    /// <exception cref="System.NotSupportedException">Thrown in cryptographic mode.</exception>
    public void Restore(FakeState state)
    {
        RequireDeterministicState("Restore");
        Guard.NotNull(state, "state");
        FakeState captured = state.Copy();
        captured.Validate();
        Engine.Restore(captured);
    }

    /// <summary>
    /// Creates an independent deterministic child stream with a stable name.
    /// </summary>
    /// <remarks>
    /// The child is derived from this stream's reset seed and does not consume
    /// this stream. In deterministic mode, repeating a name produces the same
    /// child sequence. Cryptographic child streams remain non-reproducible.
    /// </remarks>
    /// <param name="name">The nonempty child-stream name.</param>
    /// <returns>A new child stream at its initial position.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public FakeRandom Fork(string name)
    {
        return FakeRandom.FromSeed(Context, ForkSeed(name));
    }

    /// <summary>
    /// Replaces the current seed with an integer seed and restarts the stream.
    /// </summary>
    /// <param name="seed">The new seed.</param>
    public void Reseed(int seed)
    {
        Engine.Reseed(StableSeed.FromInt(seed));
    }

    /// <summary>
    /// Replaces the current seed with a long seed and restarts the stream.
    /// </summary>
    /// <param name="seed">The new seed.</param>
    public void ReseedFromSeed(long seed)
    {
        Engine.Reseed(StableSeed.FromLong(seed));
    }

    /// <summary>
    /// Replaces the current seed with a UTF-8 string seed and restarts the stream.
    /// </summary>
    /// <param name="seed">The new seed.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>seed</c> is null.</exception>
    public void ReseedFromSeedUsingString(string seed)
    {
        Engine.Reseed(StableSeed.FromString(seed));
    }

    /// <summary>
    /// Replaces the current seed with a byte-array seed and restarts the stream.
    /// </summary>
    /// <param name="seed">The new seed bytes.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>seed</c> is null.</exception>
    public void ReseedFromSeedUsingByteArray(byte[] seed)
    {
        Engine.Reseed(StableSeed.FromBytes(seed));
    }

    internal ulong ForkSeed(string name)
    {
        return StableSeed.Derive(Engine.InitialSeed, "child", name);
    }

    ulong UnsignedRange(ulong minimum, ulong maximum, bool maximumInclusive)
    {
        if (maximumInclusive)
        {
            if (maximum < minimum)
            {
                throw new System.ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than or equal to minimum when it is inclusive.");
            }

            if (minimum == 0UL && maximum == ulong.MaxValue)
            {
                return ULong();
            }

            ulong inclusiveWidth = maximum - minimum + 1UL;
            return minimum + BoundedRandom.Below(this, inclusiveWidth);
        }

        if (maximum <= minimum)
        {
            throw new System.ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than minimum when it is exclusive.");
        }

        ulong exclusiveWidth = maximum - minimum;
        return minimum + BoundedRandom.Below(this, exclusiveWidth);
    }

    long SignedRange(long minimum, long maximum, bool maximumInclusive)
    {
        ulong orderedMinimum = BoundedRandom.Ordered(minimum);
        ulong orderedMaximum = BoundedRandom.Ordered(maximum);
        if (maximumInclusive)
        {
            if (orderedMaximum < orderedMinimum)
            {
                throw new System.ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than or equal to minimum when it is inclusive.");
            }

            if (orderedMinimum == 0UL && orderedMaximum == ulong.MaxValue)
            {
                return BoundedRandom.Signed(ULong());
            }

            ulong inclusiveWidth = orderedMaximum - orderedMinimum + 1UL;
            ulong inclusiveValue = orderedMinimum + BoundedRandom.Below(this, inclusiveWidth);
            return BoundedRandom.Signed(inclusiveValue);
        }

        if (orderedMaximum <= orderedMinimum)
        {
            throw new System.ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than minimum when it is exclusive.");
        }

        ulong exclusiveWidth = orderedMaximum - orderedMinimum;
        ulong exclusiveValue = orderedMinimum + BoundedRandom.Below(this, exclusiveWidth);
        return BoundedRandom.Signed(exclusiveValue);
    }

    System.Numerics.BigInteger BigIntegerRange(System.Numerics.BigInteger minimum, System.Numerics.BigInteger maximum, bool maximumInclusive)
    {
        System.Numerics.BigInteger width;
        if (maximumInclusive)
        {
            if (maximum < minimum)
            {
                throw new System.ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than or equal to minimum when it is inclusive.");
            }

            width = maximum - minimum + System.Numerics.BigInteger.One;
        }
        else
        {
            if (maximum <= minimum)
            {
                throw new System.ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than minimum when it is exclusive.");
            }

            width = maximum - minimum;
        }

        return minimum + BoundedRandom.Below(this, width);
    }

    void RequireDeterministicState(string operation)
    {
        if (Context.CapturedOptions.RandomMode == RandomMode.Cryptographic)
        {
            throw new System.NotSupportedException($"{operation} is not supported in cryptographic random mode because the platform random-number generator does not expose restorable state.");
        }
    }
}
