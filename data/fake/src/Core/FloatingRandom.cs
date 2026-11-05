using System;
using System.Numerics;

namespace fake;

internal static class FloatingRandom
{
    internal static float Float(FakeRandom random, float minimum, float maximum, bool maximumInclusive)
    {
        Finite(minimum, "minimum");
        Finite(maximum, "maximum");
        ValidateRange(minimum, maximum, maximumInclusive);
        if (minimum == maximum)
        {
            return minimum;
        }
        float unit = FloatUnit(random, maximumInclusive);
        float result;
        if (minimum >= 0.0f)
        {
            result = minimum + ((maximum - minimum) * unit);
        }
        else if (maximum <= 0.0f)
        {
            result = maximum - ((maximum - minimum) * (1.0f - unit));
        }
        else
        {
            result = (minimum * (1.0f - unit)) + (maximum * unit);
        }
        if (result < minimum)
        {
            return minimum;
        }
        if (maximumInclusive)
        {
            if (result > maximum)
            {
                return maximum;
            }
            return result;
        }
        if (result >= maximum)
        {
            float below = MathF.BitDecrement(maximum);
            return below < minimum ? minimum : below;
        }
        return result;
    }

    internal static double Double(FakeRandom random, double minimum, double maximum, bool maximumInclusive)
    {
        Finite(minimum, "minimum");
        Finite(maximum, "maximum");
        ValidateRange(minimum, maximum, maximumInclusive);
        if (minimum == maximum)
        {
            return minimum;
        }
        double unit = DoubleUnit(random, maximumInclusive);
        double result;
        if (minimum >= 0.0)
        {
            result = minimum + ((maximum - minimum) * unit);
        }
        else if (maximum <= 0.0)
        {
            result = maximum - ((maximum - minimum) * (1.0 - unit));
        }
        else
        {
            result = (minimum * (1.0 - unit)) + (maximum * unit);
        }
        if (result < minimum)
        {
            return minimum;
        }
        if (maximumInclusive)
        {
            if (result > maximum)
            {
                return maximum;
            }
            return result;
        }
        if (result >= maximum)
        {
            double below = Math.BitDecrement(maximum);
            return below < minimum ? minimum : below;
        }
        return result;
    }

    internal static decimal Decimal(FakeRandom random, decimal minimum, decimal maximum, bool maximumInclusive)
    {
        ValidateRange(minimum, maximum, maximumInclusive);
        if (minimum == maximum)
        {
            return minimum;
        }
        decimal unit = DecimalUnit(random, maximumInclusive);
        decimal result;
        if (minimum >= decimal.Zero)
        {
            result = minimum + ((maximum - minimum) * unit);
        }
        else if (maximum <= decimal.Zero)
        {
            result = maximum - ((maximum - minimum) * (decimal.One - unit));
        }
        else
        {
            result = (minimum * (decimal.One - unit)) + (maximum * unit);
        }
        if (result < minimum)
        {
            return minimum;
        }
        if (maximumInclusive)
        {
            if (result > maximum)
            {
                return maximum;
            }
            return result;
        }
        if (result >= maximum)
        {
            return DecimalBelow(minimum, maximum);
        }
        return result;
    }

    internal static void Finite(float value, string parameterName)
    {
        if (float.IsNaN(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} cannot be NaN.");
        }
        if (float.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} must be finite.");
        }
    }

    internal static void Finite(double value, string parameterName)
    {
        if (double.IsNaN(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} cannot be NaN.");
        }
        if (double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"{parameterName} must be finite.");
        }
    }

    static float FloatUnit(FakeRandom random, bool inclusive)
    {
        if (inclusive)
        {
            uint numerator = random.UIntFromMinimumAndMaximumAndMaximumInclusive(0U, 16777215U, true);
            return (float)numerator / 16777215.0f;
        }
        return (float)random.UIntFromMaximumExclusive(16777216U) / 16777216.0f;
    }

    static double DoubleUnit(FakeRandom random, bool inclusive)
    {
        if (inclusive)
        {
            ulong numerator = random.ULongFromMinimumAndMaximumAndMaximumInclusive(0UL, 9007199254740991UL, true);
            return (double)numerator / 9007199254740991.0;
        }
        return (double)random.ULongFromMaximumExclusive(9007199254740992UL) / 9007199254740992.0;
    }

    static decimal DecimalUnit(FakeRandom random, bool inclusive)
    {
        BigInteger scale = BigInteger.Pow(new BigInteger(10), 28);
        BigInteger bound = inclusive ? scale + BigInteger.One : scale;
        BigInteger numerator = BoundedRandom.Below(random, bound, false);
        return (decimal)numerator / 10000000000000000000000000000m;
    }

    static decimal DecimalBelow(decimal minimum, decimal maximum)
    {
        decimal step = 0.0000000000000000000000000001m;
        decimal candidate = maximum - step;
        while (candidate >= maximum && step < decimal.One)
        {
            step = step * 10m;
            candidate = maximum - step;
        }
        return candidate < minimum ? minimum : candidate;
    }

    static void ValidateRange(float minimum, float maximum, bool maximumInclusive)
    {
        if (maximumInclusive)
        {
            if (maximum < minimum)
            {
                throw new ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than or equal to minimum when it is inclusive.");
            }
            return;
        }
        if (maximum <= minimum)
        {
            throw new ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than minimum when it is exclusive.");
        }
    }

    static void ValidateRange(double minimum, double maximum, bool maximumInclusive)
    {
        if (maximumInclusive)
        {
            if (maximum < minimum)
            {
                throw new ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than or equal to minimum when it is inclusive.");
            }
            return;
        }
        if (maximum <= minimum)
        {
            throw new ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than minimum when it is exclusive.");
        }
    }

    static void ValidateRange(decimal minimum, decimal maximum, bool maximumInclusive)
    {
        if (maximumInclusive)
        {
            if (maximum < minimum)
            {
                throw new ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than or equal to minimum when it is inclusive.");
            }
            return;
        }
        if (maximum <= minimum)
        {
            throw new ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than minimum when it is exclusive.");
        }
    }
}
