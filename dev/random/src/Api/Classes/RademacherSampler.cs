using System;

namespace random;

/// <summary>
/// Generates negative-one and positive-one values from a Rademacher distribution.
/// </summary>
/// <remarks>
/// Both support values have equal probability. The sampler retains the supplied mutable
/// generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class RademacherSampler : IRademacherSampler
{
    IRandomSource generator;

    /// <summary>
    /// Creates a Rademacher sampler.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits.</param>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public RademacherSampler(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        this.generator = generator;
    }

    /// <summary>
    /// Returns one value from this sampler's Rademacher distribution.
    /// </summary>
    /// <returns>Negative one or positive one with equal probability.</returns>
    public int Sample()
    {
        return (generator.NextBoolean() ? 1 : -1);
    }

    /// <summary>
    /// Fills a destination with values from this sampler's Rademacher distribution.
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
