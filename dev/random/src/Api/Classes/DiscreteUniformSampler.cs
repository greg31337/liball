using System;

namespace random;

/// <summary>
/// Generates signed 32-bit values from an inclusive discrete uniform distribution.
/// </summary>
/// <remarks>
/// Every integer from the minimum through the maximum inclusive has equal probability.
/// The full signed 32-bit domain is supported without range-width overflow. The sampler
/// retains the supplied mutable generator and is not safe for concurrent use without
/// external synchronization.
/// </remarks>
public class DiscreteUniformSampler : IDiscreteUniformSampler
{
    IRandomSource generator;
    int minimum;
    int maximum;

    /// <summary>
    /// Creates an inclusive discrete uniform sampler with the requested bounds.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits.</param>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    /// <remarks>Equal bounds define a point mass that consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, or the maximum is less than the minimum.</exception>
    public DiscreteUniformSampler(IRandomSource generator, int minimum, int maximum)
    {
        Guard.NotNull(generator, "generator");
        if (maximum < minimum)
        {
            throw RandomArgumentException.Create("maximum", "The inclusive maximum must be greater than or equal to the minimum.");
        }

        this.generator = generator;
        this.minimum = minimum;
        this.maximum = maximum;
    }

    /// <summary>
    /// Returns one value from this sampler's inclusive discrete uniform distribution.
    /// </summary>
    /// <returns>An integer between the configured minimum and maximum inclusive.</returns>
    public int Sample()
    {
        if (minimum == maximum)
        {
            return minimum;
        }

        ulong width = (ulong)((long)maximum - (long)minimum) + 1UL;
        uint offset;
        if (width == IntegerUniform.UInt32DomainSize)
        {
            offset = generator.NextUInt32();
        }
        else
        {
            offset = IntegerUniform.NextUInt32(generator, (uint)width);
        }

        long result = (long)minimum + (long)offset;
        return (int)result;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's inclusive discrete uniform distribution.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="Sample()"/>.</remarks>
    public void Fill(Span<int> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample();
            index++;
        }
    }
}
