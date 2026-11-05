using System;

namespace random;

/// <summary>
/// Generates beta-distributed double-precision values over finite bounds.
/// </summary>
/// <remarks>
/// The sampler maps a beta variate from <c>[0, 1]</c> onto the closed interval
/// <c>[minimum, maximum]</c> with overflow-safe interpolation. It retains the supplied
/// mutable generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class BetaScaledSampler : IBetaScaledSampler
{
    BetaSampler betaSampler;
    double minimum;
    double maximum;

    /// <summary>
    /// Creates a scaled beta sampler with the requested shapes and finite bounds.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="alpha">The finite positive first shape.</param>
    /// <param name="beta">The finite positive second shape.</param>
    /// <param name="minimum">The finite inclusive lower bound.</param>
    /// <param name="maximum">The finite inclusive upper bound.</param>
    /// <remarks>Equal bounds define a point mass that consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">A shape is invalid, a bound is non-finite, or the maximum is less than the minimum.</exception>
    public BetaScaledSampler(IRandomSource generator, double alpha, double beta, double minimum, double maximum)
    {
        BetaSampler.ValidateParameters(generator, alpha, beta);
        Guard.Finite(minimum, "minimum");
        Guard.Finite(maximum, "maximum");
        if (minimum > maximum)
        {
            throw RandomArgumentException.Create("maximum", "The maximum must be greater than or equal to the minimum.");
        }

        BetaSampler validatedSampler = new BetaSampler(generator, alpha, beta);
        betaSampler = validatedSampler;
        this.minimum = minimum;
        this.maximum = maximum;
    }

    /// <summary>
    /// Returns one value from this sampler's scaled beta distribution.
    /// </summary>
    /// <returns>A value in the configured closed interval, or the exact minimum for equal bounds.</returns>
    public double Sample()
    {
        if (minimum == maximum)
        {
            return minimum;
        }

        double unit = betaSampler.Sample();
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

        if (result > maximum)
        {
            return maximum;
        }

        return result;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's scaled beta distribution.
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
