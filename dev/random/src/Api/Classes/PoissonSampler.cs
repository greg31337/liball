using System;

namespace random;

/// <summary>
/// Generates signed 64-bit counts from a Poisson distribution.
/// </summary>
/// <remarks>
/// Means below 10 use product inversion. Larger means use transformed rejection with
/// stable deviance and Stirling-error calculations. The supported upper mean leaves a
/// margin below the signed 64-bit count limit for transformed-rejection candidates.
/// The sampler retains the supplied mutable generator and is not safe for concurrent
/// use without external synchronization.
/// </remarks>
public class PoissonSampler : IPoissonSampler
{
    /// <summary>
    /// The largest supported finite mean for signed 64-bit Poisson counts.
    /// </summary>
    internal const double MaximumMean = PoissonLimits.MaximumMean;
    PoissonCountSampler sampler;

    /// <summary>
    /// Creates a Poisson sampler with the requested mean.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="mean">The finite mean from zero through <see cref="MaximumMean"/> inclusive.</param>
    /// <remarks>A zero mean defines a point mass at zero that consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, or the mean is non-finite, negative, or greater than <see cref="MaximumMean"/>.</exception>
    public PoissonSampler(IRandomSource generator, double mean)
    {
        PoissonCountSampler validatedSampler = new PoissonCountSampler(generator, mean);
        sampler = validatedSampler;
    }

    /// <summary>
    /// Returns one count from this sampler's Poisson distribution.
    /// </summary>
    /// <returns>A nonnegative signed 64-bit count.</returns>
    public long Sample()
    {
        return sampler.Sample();
    }

    /// <summary>
    /// Fills a destination with counts from this sampler's Poisson distribution.
    /// </summary>
    /// <param name="destination">The destination counts.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="Sample()"/>.</remarks>
    public void Fill(Span<long> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample();
            index++;
        }
    }
}
