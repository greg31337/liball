namespace random;

internal static class FloatingUniform
{
    internal const uint SingleGridSize = 16777216U;
    internal const ulong DoubleGridSize = 9007199254740992UL;
    internal const float SingleUnit = 1.0f / 16777216.0f;
    internal const double DoubleUnit = 1.0 / 9007199254740992.0;

    internal static float NextSingleClosedOpen(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        uint numerator = generator.NextUInt32() & 0xFFFFFFU;
        return SingleFromNumerator(numerator);
    }

    internal static float NextSingleOpenOpen(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        while (true)
        {
            uint numerator = generator.NextUInt32() & 0xFFFFFFU;
            if (numerator != 0U)
            {
                return SingleFromNumerator(numerator);
            }
        }
    }

    internal static float NextSingleOpenClosed(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        uint numerator = (generator.NextUInt32() & 0xFFFFFFU) + 1U;
        return SingleFromNumerator(numerator);
    }

    internal static float NextSingleClosedClosed(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        uint numerator = IntegerUniform.NextUInt32(generator, SingleGridSize + 1U);
        return SingleFromNumerator(numerator);
    }

    internal static double NextDoubleClosedOpen(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        ulong numerator = generator.NextUInt64() & 0x1FFFFFFFFFFFFFUL;
        return DoubleFromNumerator(numerator);
    }

    internal static double NextDoubleOpenOpen(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        while (true)
        {
            ulong numerator = generator.NextUInt64() & 0x1FFFFFFFFFFFFFUL;
            if (numerator != 0UL)
            {
                return DoubleFromNumerator(numerator);
            }
        }
    }

    internal static double NextDoubleOpenClosed(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        ulong numerator = (generator.NextUInt64() & 0x1FFFFFFFFFFFFFUL) + 1UL;
        return DoubleFromNumerator(numerator);
    }

    internal static double NextDoubleClosedClosed(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        ulong numerator = IntegerUniform.NextUInt64(generator, DoubleGridSize + 1UL);
        return DoubleFromNumerator(numerator);
    }

    internal static float NextSingle(IRandomSource generator, float minimum, float maximum)
    {
        Guard.NotNull(generator, "generator");
        if (!float.IsFinite(minimum))
        {
            throw RandomArgumentException.Create("minimum", "The minimum must be finite.");
        }

        if (!float.IsFinite(maximum))
        {
            throw RandomArgumentException.Create("maximum", "The maximum must be finite.");
        }

        if (minimum == maximum)
        {
            return minimum;
        }

        if (minimum > maximum)
        {
            throw RandomArgumentException.Create("maximum", "The maximum must be greater than or equal to the minimum.");
        }

        float unit = NextSingleClosedOpen(generator);
        return InterpolateSingleClosedOpen(minimum, maximum, unit);
    }

    internal static double NextDouble(IRandomSource generator, double minimum, double maximum)
    {
        Guard.NotNull(generator, "generator");
        if (!double.IsFinite(minimum))
        {
            throw RandomArgumentException.Create("minimum", "The minimum must be finite.");
        }

        if (!double.IsFinite(maximum))
        {
            throw RandomArgumentException.Create("maximum", "The maximum must be finite.");
        }

        if (minimum == maximum)
        {
            return minimum;
        }

        if (minimum > maximum)
        {
            throw RandomArgumentException.Create("maximum", "The maximum must be greater than or equal to the minimum.");
        }

        double unit = NextDoubleClosedOpen(generator);
        return InterpolateDoubleClosedOpen(minimum, maximum, unit);
    }

    internal static float InterpolateSingleClosedOpen(float minimum, float maximum, float unit)
    {
        float width = maximum - minimum;
        float result;
        if (float.IsInfinity(width))
        {
            result = minimum * (1.0f - unit) + maximum * unit;
        }
        else
        {
            result = minimum + width * unit;
        }

        if (result < minimum)
        {
            return minimum;
        }

        if (result >= maximum)
        {
            return PortableMath.NextDown(maximum);
        }

        return result;
    }

    internal static double InterpolateDoubleClosedOpen(double minimum, double maximum, double unit)
    {
        double width = maximum - minimum;
        double result;
        if (double.IsInfinity(width))
        {
            result = minimum * (1.0 - unit) + maximum * unit;
        }
        else
        {
            result = minimum + width * unit;
        }

        if (result < minimum)
        {
            return minimum;
        }

        if (result >= maximum)
        {
            return PortableMath.NextDown(maximum);
        }

        return result;
    }

    internal static float SingleFromNumerator(uint numerator)
    {
        Guard.Argument(numerator <= SingleGridSize, "numerator", "The numerator must fit the single-precision unit grid.");
        return (float)numerator * SingleUnit;
    }

    internal static double DoubleFromNumerator(ulong numerator)
    {
        Guard.Argument(numerator <= DoubleGridSize, "numerator", "The numerator must fit the double-precision unit grid.");
        return (double)numerator * DoubleUnit;
    }
}
