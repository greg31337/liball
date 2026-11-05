using System;

namespace random;

internal class PoissonCountSampler
{
    internal const double MaximumMean = 9.2233720064847708e18;
    const double HalfLogTwoPi = 0.91893853320467274178;
    const double FirstStirlingCoefficient = 0.083333333333333333333333333333333333;
    const double SecondStirlingCoefficient = 0.002777777777777777777777777777777778;
    const double ThirdStirlingCoefficient = 0.000793650793650793650793650793650794;
    const double FourthStirlingCoefficient = 0.000595238095238095238095238095238095;
    const double FifthStirlingCoefficient = 0.000841750841750841750841750841750842;
    IRandomSource generator;
    double mean;
    double exponentialLimit;
    double rootMean;
    double logMean;
    double firstCoefficient;
    double secondCoefficient;
    double inverseAlpha;
    double fastAcceptance;
    RandomMathMode mathMode;

    internal PoissonCountSampler(IRandomSource generator, double mean)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(mean, "mean");
        if (mean < 0.0 || mean > MaximumMean)
        {
            throw RandomArgumentException.Create("mean", "The Poisson mean must be within the safe signed 64-bit count range.");
        }

        this.generator = generator;
        this.mean = mean;
        mathMode = generator.MathMode();
        exponentialLimit = 0.0;
        rootMean = 0.0;
        logMean = 0.0;
        firstCoefficient = 0.0;
        secondCoefficient = 0.0;
        inverseAlpha = 0.0;
        fastAcceptance = 0.0;

        if (mean < 10.0)
        {
            exponentialLimit = Exp(-mean);
        }
        else
        {
            rootMean = Sqrt(mean);
            logMean = Log(mean);
            secondCoefficient = 0.931 + 2.53 * rootMean;
            firstCoefficient = -0.059 + 0.02483 * secondCoefficient;
            inverseAlpha = 1.1239 + 1.1328 / (secondCoefficient - 3.4);
            fastAcceptance = 0.9277 - 3.6224 / (secondCoefficient - 2.0);
        }
    }

    internal long Sample()
    {
        if (mean == 0.0)
        {
            return 0L;
        }

        if (mean < 10.0)
        {
            return SampleProduct();
        }

        return SampleTransformedRejection();
    }

    long SampleProduct()
    {
        long count = 0L;
        double product = 1.0;
        while (true)
        {
            product *= FloatingUniform.NextDoubleOpenOpen(generator);
            if (product <= exponentialLimit)
            {
                return count;
            }

            count++;
        }
    }

    long SampleTransformedRejection()
    {
        while (true)
        {
            double centeredUnit = FloatingUniform.NextDoubleOpenOpen(generator) - 0.5;
            double secondUnit = FloatingUniform.NextDoubleOpenOpen(generator);
            double distance = 0.5 - Math.Abs(centeredUnit);
            double candidateValue = (2.0 * firstCoefficient / distance + secondCoefficient) * centeredUnit + mean + 0.43;
            if (candidateValue < 0.0 || candidateValue >= (double)long.MaxValue)
            {
                continue;
            }

            long candidate = (long)Math.Floor(candidateValue);
            if (distance >= 0.07 && secondUnit <= fastAcceptance)
            {
                return candidate;
            }

            if (distance < 0.013 && secondUnit > distance)
            {
                continue;
            }

            double left = Log(secondUnit) + Log(inverseAlpha) -
                Log(firstCoefficient / (distance * distance) + secondCoefficient);
            double right = PoissonLogProbability((double)candidate);
            if (left <= right)
            {
                return candidate;
            }
        }
    }

    double PoissonLogProbability(double count)
    {
        if (count == 0.0)
        {
            return -mean;
        }

        return -StirlingError(count) - DeviancePart(count, mean) - HalfLogTwoPi - 0.5 * Log(count);
    }

    double StirlingError(double value)
    {
        if (value < 16.0)
        {
            return PortableSpecialFunctions.LogGamma(value + 1.0) -
                (value + 0.5) * Log(value) + value - HalfLogTwoPi;
        }

        double square = value * value;
        return (FirstStirlingCoefficient -
            (SecondStirlingCoefficient -
            (ThirdStirlingCoefficient -
            (FourthStirlingCoefficient - FifthStirlingCoefficient / square) / square) / square) / square) / value;
    }

    double DeviancePart(double value, double reference)
    {
        double difference = value - reference;
        if (Math.Abs(difference) < 0.1 * (value + reference))
        {
            double ratio = difference / (value + reference);
            double sum = difference * ratio;
            double term = 2.0 * value * ratio;
            double squaredRatio = ratio * ratio;
            int denominator = 3;
            while (denominator <= 1001)
            {
                term *= squaredRatio;
                double next = sum + term / denominator;
                if (next == sum)
                {
                    return next;
                }

                sum = next;
                denominator += 2;
            }

            return sum;
        }

        return value * Log(value / reference) + reference - value;
    }

    double Sqrt(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Sqrt(value);
        }

        return Math.Sqrt(value);
    }

    double Log(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Log(value);
        }

        return Math.Log(value);
    }

    double Exp(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Exp(value);
        }

        return Math.Exp(value);
    }
}
