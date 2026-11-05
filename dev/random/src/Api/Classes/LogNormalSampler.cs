using System;

namespace random;

/// <summary>
/// Generates double-precision values from a log-normal distribution.
/// </summary>
/// <remarks>
/// The configured parameters are the mean and standard deviation of the underlying
/// normal variate. The sampler retains the supplied mutable generator and is not safe
/// for concurrent use without external synchronization.
/// </remarks>
public class LogNormalSampler : ILogNormalSampler
{
    NormalSampler normalSampler;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a log-normal sampler from the requested underlying normal parameters.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="logMean">The finite mean of the underlying normal variate.</param>
    /// <param name="logStandardDeviation">The finite nonnegative standard deviation of the underlying normal variate.</param>
    /// <remarks>
    /// A zero log-standard-deviation defines a point mass and consumes no random state.
    /// Exponential overflow returns positive infinity and underflow returns zero.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, a parameter is non-finite, or the log-standard-deviation is negative.</exception>
    public LogNormalSampler(IRandomSource generator, double logMean, double logStandardDeviation)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(logMean, "logMean");
        Guard.Finite(logStandardDeviation, "logStandardDeviation");
        if (logStandardDeviation < 0.0)
        {
            throw RandomArgumentException.Create("logStandardDeviation", "The log-standard-deviation must not be negative.");
        }

        NormalSampler validatedSampler = new NormalSampler(generator, logMean, logStandardDeviation);
        normalSampler = validatedSampler;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's log-normal distribution.
    /// </summary>
    /// <returns>A nonnegative log-normal value, positive infinity after overflow, or zero after underflow.</returns>
    public double Sample()
    {
        double exponent = normalSampler.Sample();
        if (exponent == double.PositiveInfinity)
        {
            return double.PositiveInfinity;
        }

        if (exponent == double.NegativeInfinity)
        {
            return 0.0;
        }

        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Exp(exponent);
        }

        return Math.Exp(exponent);
    }

    /// <summary>
    /// Fills a destination with values from this sampler's log-normal distribution.
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
}
