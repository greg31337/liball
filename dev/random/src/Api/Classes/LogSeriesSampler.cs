using System;

namespace random;

/// <summary>
/// Generates positive signed 64-bit counts from a logarithmic-series distribution.
/// </summary>
/// <remarks>
/// For probability <c>p</c>, positive integer <c>k</c> has mass
/// <c>-p^k / (k * log(1-p))</c>. Probability zero is interpreted as the limiting
/// point mass at one. Sampling uses Kemp's rejection algorithm with stable
/// <c>log1p</c> and <c>expm1</c> evaluations near both probability boundaries.
/// The sampler retains the supplied mutable generator and is not safe for concurrent
/// use without external synchronization.
/// </remarks>
public class LogSeriesSampler : ILogSeriesSampler
{
    const double LongLimit = 9223372036854775808.0;
    IRandomSource generator;
    double probability;
    double logComplement;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a logarithmic-series sampler with the requested probability.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="probability">The finite probability greater than or equal to zero and less than one.</param>
    /// <remarks>A probability of zero defines a point mass at one and consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, or the probability is non-finite or outside the interval <c>[0, 1)</c>.</exception>
    public LogSeriesSampler(IRandomSource generator, double probability)
    {
        Guard.NotNull(generator, "generator");
        Guard.Probability(probability, "probability");
        if (probability == 1.0)
        {
            throw RandomArgumentException.Create("probability", "The logarithmic-series probability must be less than one.");
        }

        this.generator = generator;
        this.probability = probability;
        mathMode = generator.MathMode();
        logComplement = probability == 0.0 ? 0.0 : Log1p(-probability);
    }

    /// <summary>
    /// Returns one count from this sampler's logarithmic-series distribution.
    /// </summary>
    /// <returns>A positive signed 64-bit count.</returns>
    /// <exception cref="RandomExhaustedException">An accepted count exceeds the signed 64-bit output capacity.</exception>
    public long Sample()
    {
        if (probability == 0.0)
        {
            return 1L;
        }

        while (true)
        {
            double comparison = FloatingUniform.NextDoubleOpenOpen(generator);
            if (comparison >= probability)
            {
                return 1L;
            }

            double unit = FloatingUniform.NextDoubleOpenOpen(generator);
            double quotient = -Expm1(logComplement * unit);
            if (comparison <= quotient * quotient)
            {
                double candidate = Math.Floor(1.0 + Log(comparison) / Log(quotient));
                if (!double.IsFinite(candidate) || candidate < 1.0 || candidate >= LongLimit)
                {
                    throw RandomExhaustedException.Create("The logarithmic-series count exceeds the signed 64-bit output capacity.");
                }

                return (long)candidate;
            }

            if (comparison >= quotient)
            {
                return 1L;
            }

            return 2L;
        }
    }

    /// <summary>
    /// Fills a destination with counts from this sampler's logarithmic-series distribution.
    /// </summary>
    /// <param name="destination">The destination counts.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="Sample()"/>.</remarks>
    /// <exception cref="RandomExhaustedException">An accepted count exceeds the signed 64-bit output capacity.</exception>
    public void Fill(Span<long> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample();
            index++;
        }
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

        if (value < -0.5 || value > 0.5)
        {
            return Math.Log(1.0 + value);
        }

        double reduced = value / (2.0 + value);
        double square = reduced * reduced;
        double term = reduced;
        double sum = term;
        int denominator = 3;
        while (denominator <= 39)
        {
            term *= square;
            sum += term / denominator;
            denominator += 2;
        }

        return 2.0 * sum;
    }

    double Expm1(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Expm1(value);
        }

        if (value >= -0.5 && value <= 0.5)
        {
            double term = value;
            double sum = value;
            int index = 2;
            while (index <= 24)
            {
                term = term * value / (double)index;
                sum += term;
                index++;
            }

            return sum;
        }

        return Math.Exp(value) - 1.0;
    }
}
