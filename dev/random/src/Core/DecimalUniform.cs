using System;

namespace random;

internal static class DecimalUniform
{
    internal const byte GridScale = 28;

    internal static decimal NextDecimal(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        while (true)
        {
            UInt128 sample = NextUInt96(generator);
            if (IsAcceptedSample(sample))
            {
                UInt128 coefficient = sample % GridSize();
                return DecimalFromCoefficient(coefficient);
            }
        }
    }

    internal static decimal NextDecimal(IRandomSource generator, decimal minimum, decimal maximum)
    {
        Guard.NotNull(generator, "generator");
        if (minimum == maximum)
        {
            return minimum;
        }

        if (minimum > maximum)
        {
            throw RandomArgumentException.Create("maximum", "The maximum must be greater than or equal to the minimum.");
        }

        decimal unit = NextDecimal(generator);
        return InterpolateClosedOpen(minimum, maximum, unit);
    }

    internal static decimal InterpolateClosedOpen(decimal minimum, decimal maximum, decimal unit)
    {
        decimal result;
        if (minimum < decimal.Zero && maximum > decimal.Zero)
        {
            result = minimum * (decimal.One - unit) + maximum * unit;
        }
        else
        {
            decimal width = maximum - minimum;
            result = minimum + width * unit;
        }

        if (result < minimum)
        {
            return minimum;
        }

        if (result >= maximum)
        {
            return Previous(maximum);
        }

        return result;
    }

    internal static UInt128 NextUInt96(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        ulong low = generator.NextUInt64();
        uint high = generator.NextUInt32();
        return ((UInt128)high << 64) | (UInt128)low;
    }

    internal static bool IsAcceptedSample(UInt128 sample)
    {
        return sample < AcceptanceLimit();
    }

    internal static UInt128 GridSize()
    {
        return (UInt128)10000000000000000000UL * (UInt128)1000000000U;
    }

    internal static UInt128 AcceptanceLimit()
    {
        return GridSize() * (UInt128)7;
    }

    internal static UInt128 UInt96DomainSize()
    {
        return (UInt128)1 << 96;
    }

    internal static decimal DecimalFromCoefficient(UInt128 coefficient)
    {
        Guard.Argument(coefficient < GridSize(), "coefficient", "The coefficient must fit the unit decimal grid.");
        return DecimalFromParts(coefficient, false, GridScale);
    }

    internal static decimal Previous(decimal value)
    {
        int[] bits = decimal.GetBits(value);
        uint low = uint.CreateTruncating(bits[0]);
        uint middle = uint.CreateTruncating(bits[1]);
        uint high = uint.CreateTruncating(bits[2]);
        bool negative = (bits[3] & int.MinValue) != 0;
        int scale = (bits[3] >> 16) & 0x7F;
        UInt128 coefficient = ((UInt128)high << 64) | ((UInt128)middle << 32) | (UInt128)low;
        UInt128 maximumCoefficient = ((UInt128)1 << 96) - (UInt128)1;

        while (scale < 28 && coefficient <= maximumCoefficient / (UInt128)10)
        {
            coefficient *= (UInt128)10;
            scale++;
        }

        if (coefficient == (UInt128)0)
        {
            coefficient = (UInt128)1;
            negative = true;
            scale = 28;
        }
        else if (!negative)
        {
            coefficient--;
        }
        else if (coefficient < maximumCoefficient)
        {
            coefficient++;
        }
        else
        {
            if (scale == 0)
            {
                return decimal.MinValue;
            }

            coefficient = coefficient / (UInt128)10 + (UInt128)1;
            scale--;
        }

        return DecimalFromParts(coefficient, negative, (byte)scale);
    }

    internal static decimal DecimalFromParts(UInt128 coefficient, bool negative, byte scale)
    {
        uint low = (uint)(coefficient & (UInt128)uint.MaxValue);
        uint middle = (uint)((coefficient >> 32) & (UInt128)uint.MaxValue);
        uint high = (uint)((coefficient >> 64) & (UInt128)uint.MaxValue);
        return new decimal(
            int.CreateTruncating(low),
            int.CreateTruncating(middle),
            int.CreateTruncating(high),
            negative,
            scale);
    }
}
