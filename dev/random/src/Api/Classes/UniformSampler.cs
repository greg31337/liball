using System;

namespace random;

/// <summary>
/// Generates double-precision values from a continuous uniform distribution.
/// </summary>
/// <remarks>
/// Values use the closed-open interval <c>[minimum, maximum)</c>. The sampler retains
/// the supplied mutable generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class UniformSampler : IUniformSampler
{
    IRandomSource generator;
    double minimum;
    double maximum;

    /// <summary>
    /// Creates a continuous uniform sampler for the requested finite bounds.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits.</param>
    /// <param name="minimum">The finite inclusive lower bound.</param>
    /// <param name="maximum">The finite exclusive upper bound.</param>
    /// <remarks>Equal bounds define a point mass that consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, a bound is non-finite, or the maximum is less than the minimum.</exception>
    public UniformSampler(IRandomSource generator, double minimum, double maximum)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(minimum, "minimum");
        Guard.Finite(maximum, "maximum");
        if (minimum > maximum)
        {
            throw RandomArgumentException.Create("maximum", "The maximum must be greater than or equal to the minimum.");
        }

        this.generator = generator;
        this.minimum = minimum;
        this.maximum = maximum;
    }

    /// <summary>
    /// Returns one value from this sampler's continuous uniform distribution.
    /// </summary>
    /// <returns>A value in the configured closed-open interval, or the exact minimum for equal bounds.</returns>
    public double Sample()
    {
        return FloatingUniform.NextDouble(generator, minimum, maximum);
    }

    /// <summary>
    /// Fills a destination with values from this sampler's continuous uniform distribution.
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
