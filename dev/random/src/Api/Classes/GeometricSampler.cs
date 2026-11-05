using System;

namespace random;

/// <summary>
/// Generates signed 64-bit counts from a geometric distribution.
/// </summary>
/// <remarks>
/// The two public conventions are named explicitly: failures before the first success
/// have support starting at zero, while trials through the first success have support
/// starting at one. Sampling uses an open-closed unit variate and a stable
/// <c>log1p(-probability)</c> transform. The sampler retains the supplied mutable generator
/// and is not safe for concurrent use without external synchronization.
/// </remarks>
public class GeometricSampler : IGeometricSampler
{
    const double LongLimit = 9223372036854775808.0;
    IRandomSource generator;
    double probability;
    double logFailureProbability;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a geometric sampler with the requested success probability.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="probability">The finite success probability greater than zero and at most one.</param>
    /// <remarks>A probability of one defines an immediate success and consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, or the probability is non-finite or outside the interval <c>(0, 1]</c>.</exception>
    public GeometricSampler(IRandomSource generator, double probability)
    {
        Guard.NotNull(generator, "generator");
        Guard.Probability(probability, "probability");
        if (probability == 0.0)
        {
            throw RandomArgumentException.Create("probability", "The success probability must be greater than zero.");
        }

        this.generator = generator;
        this.probability = probability;
        mathMode = generator.MathMode();
        logFailureProbability = probability == 1.0 ? 0.0 : Log1p(-probability);
    }

    /// <summary>
    /// Returns the number of failures before the first success.
    /// </summary>
    /// <returns>A nonnegative signed 64-bit failure count.</returns>
    /// <exception cref="RandomExhaustedException">The sampled failure count exceeds the signed 64-bit output capacity.</exception>
    public long SampleFailures()
    {
        if (probability == 1.0)
        {
            return 0L;
        }

        return SampleFailureCount(false);
    }

    /// <summary>
    /// Returns the number of trials through and including the first success.
    /// </summary>
    /// <returns>A positive signed 64-bit trial count.</returns>
    /// <exception cref="RandomExhaustedException">The sampled trial count exceeds the signed 64-bit output capacity.</exception>
    public long SampleTrials()
    {
        if (probability == 1.0)
        {
            return 1L;
        }

        long failures = SampleFailureCount(true);
        return failures + 1L;
    }

    /// <summary>
    /// Fills a destination with failure counts from this sampler's geometric distribution.
    /// </summary>
    /// <param name="destination">The destination failure counts.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="SampleFailures()"/>.</remarks>
    /// <exception cref="RandomExhaustedException">A sampled failure count exceeds the signed 64-bit output capacity.</exception>
    public void FillFailures(Span<long> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = SampleFailures();
            index++;
        }
    }

    /// <summary>
    /// Fills a destination with trial counts from this sampler's geometric distribution.
    /// </summary>
    /// <param name="destination">The destination trial counts.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="SampleTrials()"/>.</remarks>
    /// <exception cref="RandomExhaustedException">A sampled trial count exceeds the signed 64-bit output capacity.</exception>
    public void FillTrials(Span<long> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = SampleTrials();
            index++;
        }
    }

    long SampleFailureCount(bool reserveTrial)
    {
        double unit = FloatingUniform.NextDoubleOpenClosed(generator);
        double count = Math.Floor(Log(unit) / logFailureProbability);
        if (!double.IsFinite(count) || count < 0.0 || count >= LongLimit)
        {
            if (reserveTrial)
            {
                throw RandomExhaustedException.Create("The geometric trial count exceeds the signed 64-bit output capacity.");
            }

            throw RandomExhaustedException.Create("The geometric failure count exceeds the signed 64-bit output capacity.");
        }

        long result = (long)count;
        if (reserveTrial && result == long.MaxValue)
        {
            throw RandomExhaustedException.Create("The geometric trial count exceeds the signed 64-bit output capacity.");
        }

        return result;
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
}
