namespace random;

internal class PortableSpecialFunctions
{
    internal const double RelativeError = 5.0e-13;
    internal const double AbsoluteError = 5.0e-14;
    const double Pi = 3.14159265358979323846e+00;
    const double HalfLogTwoPi = 9.1893853320467274178e-01;
    const double LogPi = 1.1447298858494001741e+00;
    const double LanczosBase = 9.9999999999980993263e-01;
    const double LanczosOne = 6.7652036812188510986e+02;
    const double LanczosTwo = -1.2591392167224028705e+03;
    const double LanczosThree = 7.7132342877765313038e+02;
    const double LanczosFour = -1.7661502916214059044e+02;
    const double LanczosFive = 1.2507343278686904814e+01;
    const double LanczosSix = -1.3857109526572011689e-01;
    const double LanczosSeven = 9.9843695780195708596e-06;
    const double LanczosEight = 1.5056327351493115583e-07;
    const double ConvergenceThreshold = 1.7763568394002504647e-15;
    const double ContinuedFractionMinimum = 1.0e-300;
    const int MaximumIterations = 10000;

    internal static double LogGamma(double value)
    {
        if (double.IsNaN(value) || value <= 0.0)
        {
            return PortableMath.DoubleFromBits(PortableMath.DoubleCanonicalNaN);
        }

        if (double.IsPositiveInfinity(value))
        {
            return double.PositiveInfinity;
        }

        if (value == 1.0 || value == 2.0)
        {
            return 0.0;
        }

        if (value < 0.5)
        {
            double sine = PortableMath.Sin(Pi * value);
            return LogPi - PortableMath.Log(sine) - LogGamma(1.0 - value);
        }

        double shifted = value - 1.0;
        double sum = LanczosBase;
        sum += LanczosOne / (shifted + 1.0);
        sum += LanczosTwo / (shifted + 2.0);
        sum += LanczosThree / (shifted + 3.0);
        sum += LanczosFour / (shifted + 4.0);
        sum += LanczosFive / (shifted + 5.0);
        sum += LanczosSix / (shifted + 6.0);
        sum += LanczosSeven / (shifted + 7.0);
        sum += LanczosEight / (shifted + 8.0);
        double scale = shifted + 7.5;
        return HalfLogTwoPi + (shifted + 0.5) * PortableMath.Log(scale) - scale + PortableMath.Log(sum);
    }

    internal static double LogBeta(double firstShape, double secondShape)
    {
        if (!ValidShape(firstShape) || !ValidShape(secondShape))
        {
            return PortableMath.DoubleFromBits(PortableMath.DoubleCanonicalNaN);
        }

        return LogGamma(firstShape) + LogGamma(secondShape) - LogGamma(firstShape + secondShape);
    }

    internal static double RegularizedGammaP(double shape, double value)
    {
        if (!ValidShape(shape) || double.IsNaN(value) || value < 0.0)
        {
            return PortableMath.DoubleFromBits(PortableMath.DoubleCanonicalNaN);
        }

        if (value == 0.0)
        {
            return 0.0;
        }

        if (double.IsPositiveInfinity(value))
        {
            return 1.0;
        }

        if (value < shape + 1.0)
        {
            return NormalizeProbability(GammaSeries(shape, value));
        }

        return NormalizeProbability(1.0 - GammaContinuedFraction(shape, value));
    }

    internal static double RegularizedGammaQ(double shape, double value)
    {
        if (!ValidShape(shape) || double.IsNaN(value) || value < 0.0)
        {
            return PortableMath.DoubleFromBits(PortableMath.DoubleCanonicalNaN);
        }

        if (value == 0.0)
        {
            return 1.0;
        }

        if (double.IsPositiveInfinity(value))
        {
            return 0.0;
        }

        if (value < shape + 1.0)
        {
            return NormalizeProbability(1.0 - GammaSeries(shape, value));
        }

        return NormalizeProbability(GammaContinuedFraction(shape, value));
    }

    internal static double RegularizedBeta(double value, double firstShape, double secondShape)
    {
        if (!ValidShape(firstShape) || !ValidShape(secondShape) || double.IsNaN(value) || value < 0.0 || value > 1.0)
        {
            return PortableMath.DoubleFromBits(PortableMath.DoubleCanonicalNaN);
        }

        if (value == 0.0)
        {
            return 0.0;
        }

        if (value == 1.0)
        {
            return 1.0;
        }

        double logarithm = -LogBeta(firstShape, secondShape);
        logarithm += firstShape * PortableMath.Log(value);
        logarithm += secondShape * PortableMath.Log1p(-value);
        double factor = PortableMath.Exp(logarithm);
        double result;
        if (value < (firstShape + 1.0) / (firstShape + secondShape + 2.0))
        {
            result = factor * BetaContinuedFraction(firstShape, secondShape, value) / firstShape;
        }
        else
        {
            result = 1.0 - factor * BetaContinuedFraction(secondShape, firstShape, 1.0 - value) / secondShape;
        }

        return NormalizeProbability(result);
    }

    internal static double Erf(double value)
    {
        if (double.IsNaN(value))
        {
            return PortableMath.DoubleFromBits(PortableMath.DoubleCanonicalNaN);
        }

        ulong bits = PortableMath.DoubleBits(value);
        ulong magnitudeBits = bits & ~PortableMath.DoubleSignMask;
        if (magnitudeBits == 0UL)
        {
            return value;
        }

        double magnitude = PortableMath.DoubleFromBits(magnitudeBits);
        double result = RegularizedGammaP(0.5, magnitude * magnitude);
        return (bits & PortableMath.DoubleSignMask) != 0UL ? -result : result;
    }

    internal static double Erfc(double value)
    {
        if (double.IsNaN(value))
        {
            return PortableMath.DoubleFromBits(PortableMath.DoubleCanonicalNaN);
        }

        ulong bits = PortableMath.DoubleBits(value);
        double magnitude = PortableMath.DoubleFromBits(bits & ~PortableMath.DoubleSignMask);
        double result = RegularizedGammaQ(0.5, magnitude * magnitude);
        return (bits & PortableMath.DoubleSignMask) != 0UL ? 2.0 - result : result;
    }

    internal static bool ValidShape(double value)
    {
        return value > 0.0 && !double.IsPositiveInfinity(value);
    }

    internal static double GammaSeries(double shape, double value)
    {
        double sum = 1.0 / shape;
        double term = sum;
        double denominator = shape;
        int iteration = 1;
        while (iteration <= MaximumIterations)
        {
            denominator += 1.0;
            term *= value / denominator;
            sum += term;
            if (Absolute(term) <= Absolute(sum) * ConvergenceThreshold)
            {
                break;
            }

            iteration++;
        }

        double exponent = shape * PortableMath.Log(value) - value - LogGamma(shape);
        return sum * PortableMath.Exp(exponent);
    }

    internal static double GammaContinuedFraction(double shape, double value)
    {
        double offset = value + 1.0 - shape;
        double first = 1.0 / ContinuedFractionMinimum;
        double second = 1.0 / SafeDenominator(offset);
        double result = second;
        int iteration = 1;
        while (iteration <= MaximumIterations)
        {
            double iterationValue = (double)iteration;
            double numerator = -iterationValue * (iterationValue - shape);
            offset += 2.0;
            second = SafeDenominator(numerator * second + offset);
            first = SafeDenominator(offset + numerator / first);
            second = 1.0 / second;
            double delta = second * first;
            result *= delta;
            if (Absolute(delta - 1.0) <= ConvergenceThreshold)
            {
                break;
            }

            iteration++;
        }

        double exponent = shape * PortableMath.Log(value) - value - LogGamma(shape);
        return PortableMath.Exp(exponent) * result;
    }

    internal static double BetaContinuedFraction(double firstShape, double secondShape, double value)
    {
        double sum = firstShape + secondShape;
        double first = 1.0;
        double second = 1.0 - sum * value / (firstShape + 1.0);
        second = 1.0 / SafeDenominator(second);
        double result = second;
        int iteration = 1;
        while (iteration <= MaximumIterations)
        {
            double iterationValue = (double)iteration;
            double twice = 2.0 * iterationValue;
            double numerator = iterationValue * (secondShape - iterationValue) * value;
            numerator /= (firstShape + twice - 1.0) * (firstShape + twice);
            second = 1.0 / SafeDenominator(1.0 + numerator * second);
            first = SafeDenominator(1.0 + numerator / first);
            result *= second * first;

            numerator = -(firstShape + iterationValue) * (sum + iterationValue) * value;
            numerator /= (firstShape + twice) * (firstShape + twice + 1.0);
            second = 1.0 / SafeDenominator(1.0 + numerator * second);
            first = SafeDenominator(1.0 + numerator / first);
            double delta = second * first;
            result *= delta;
            if (Absolute(delta - 1.0) <= ConvergenceThreshold)
            {
                break;
            }

            iteration++;
        }

        return result;
    }

    internal static double SafeDenominator(double value)
    {
        if (Absolute(value) >= ContinuedFractionMinimum)
        {
            return value;
        }

        return value < 0.0 ? -ContinuedFractionMinimum : ContinuedFractionMinimum;
    }

    internal static double NormalizeProbability(double value)
    {
        if (value < 0.0)
        {
            return 0.0;
        }

        if (value > 1.0)
        {
            return 1.0;
        }

        return value;
    }

    internal static double Absolute(double value)
    {
        return value < 0.0 ? -value : value;
    }
}
