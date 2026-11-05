using System;

namespace random;

/// <summary>
/// Generates double-precision values from a beta distribution.
/// </summary>
/// <remarks>
/// Shapes at most one use log-domain Johnk rejection. Other regimes use independent
/// gamma variates and a ratio that never forms their potentially overflowing sum.
/// The sampler retains the supplied mutable generator and is not safe for concurrent
/// use without external synchronization.
/// </remarks>
public class BetaSampler : IBetaSampler
{
    IRandomSource generator;
    GammaSampler firstGamma;
    GammaSampler secondGamma;
    double alpha;
    double beta;
    double logAlpha;
    double logBeta;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a beta sampler with the requested positive shapes.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="alpha">The finite positive first shape.</param>
    /// <param name="beta">The finite positive second shape.</param>
    /// <remarks>Representational underflow for highly asymmetric shapes can return an exact zero or one.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, or a shape is non-finite or not positive.</exception>
    public BetaSampler(IRandomSource generator, double alpha, double beta)
    {
        ValidateParameters(generator, alpha, beta);
        RandomMathMode selectedMathMode = generator.MathMode();
        GammaSampler validatedFirstGamma = new GammaSampler(generator, alpha, 1.0);
        GammaSampler validatedSecondGamma = new GammaSampler(generator, beta, 1.0);
        double selectedLogAlpha;
        double selectedLogBeta;
        if (selectedMathMode == RandomMathMode.Portable)
        {
            selectedLogAlpha = PortableMath.Log(alpha);
            selectedLogBeta = PortableMath.Log(beta);
        }
        else
        {
            selectedLogAlpha = Math.Log(alpha);
            selectedLogBeta = Math.Log(beta);
        }

        this.generator = generator;
        firstGamma = validatedFirstGamma;
        secondGamma = validatedSecondGamma;
        this.alpha = alpha;
        this.beta = beta;
        logAlpha = selectedLogAlpha;
        logBeta = selectedLogBeta;
        mathMode = selectedMathMode;
    }

    /// <summary>
    /// Returns one value from this sampler's beta distribution.
    /// </summary>
    /// <returns>A value in the closed interval <c>[0, 1]</c>.</returns>
    public double Sample()
    {
        if (alpha <= 1.0 && beta <= 1.0)
        {
            return SampleSmallShapes();
        }

        double first = firstGamma.Sample();
        double second = secondGamma.Sample();
        return Ratio(first, second, alpha, beta);
    }

    /// <summary>
    /// Fills a destination with values from this sampler's beta distribution.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="Sample()"/>.</remarks>
    public void Fill(Span<double> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample();
            index++;
        }
    }

    double SampleSmallShapes()
    {
        while (true)
        {
            double firstUniformLog = Log(FloatingUniform.NextDoubleOpenOpen(generator));
            double secondUniformLog = Log(FloatingUniform.NextDoubleOpenOpen(generator));
            double firstLog = firstUniformLog / alpha;
            double secondLog = secondUniformLog / beta;
            if (firstLog == double.NegativeInfinity && secondLog == double.NegativeInfinity)
            {
                double firstScore = Log(-firstUniformLog) - logAlpha;
                double secondScore = Log(-secondUniformLog) - logBeta;
                if (firstScore == secondScore)
                {
                    return 0.5;
                }

                return firstScore < secondScore ? 1.0 : 0.0;
            }

            double maximumLog = firstLog > secondLog ? firstLog : secondLog;
            double minimumLog = firstLog > secondLog ? secondLog : firstLog;
            double sumLog = maximumLog + Log1p(Exp(minimumLog - maximumLog));
            if (sumLog <= 0.0)
            {
                return RatioFromLogs(firstLog, secondLog);
            }
        }
    }

    double RatioFromLogs(double firstLog, double secondLog)
    {
        if (firstLog == double.NegativeInfinity)
        {
            return 0.0;
        }

        if (secondLog == double.NegativeInfinity)
        {
            return 1.0;
        }

        if (firstLog >= secondLog)
        {
            return 1.0 / (1.0 + Exp(secondLog - firstLog));
        }

        double ratio = Exp(firstLog - secondLog);
        return ratio / (1.0 + ratio);
    }

    double Log(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Log(value);
        }

        return Math.Log(value);
    }

    double Log1p(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Log1p(value);
        }

        return Math.Log(1.0 + value);
    }

    double Exp(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Exp(value);
        }

        return Math.Exp(value);
    }

    internal static void ValidateParameters(IRandomSource generator, double alpha, double beta)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(alpha, "alpha");
        if (alpha <= 0.0)
        {
            throw RandomArgumentException.Create("alpha", "The first shape must be positive.");
        }

        Guard.Finite(beta, "beta");
        if (beta <= 0.0)
        {
            throw RandomArgumentException.Create("beta", "The second shape must be positive.");
        }
    }

    static double Ratio(double first, double second, double alpha, double beta)
    {
        if (first == 0.0 && second == 0.0)
        {
            return ShapeRatio(alpha, beta);
        }

        if (first == double.PositiveInfinity && second == double.PositiveInfinity)
        {
            return ShapeRatio(alpha, beta);
        }

        if (first == 0.0 || second == double.PositiveInfinity)
        {
            return 0.0;
        }

        if (second == 0.0 || first == double.PositiveInfinity)
        {
            return 1.0;
        }

        if (first == second)
        {
            return 0.5;
        }

        if (first > second)
        {
            return 1.0 / (1.0 + second / first);
        }

        double ratio = first / second;
        return ratio / (1.0 + ratio);
    }

    static double ShapeRatio(double alpha, double beta)
    {
        if (alpha == beta)
        {
            return 0.5;
        }

        if (alpha > beta)
        {
            return 1.0 / (1.0 + beta / alpha);
        }

        double ratio = alpha / beta;
        return ratio / (1.0 + ratio);
    }
}
