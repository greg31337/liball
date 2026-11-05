using System;

namespace random;

/// <summary>
/// Generates Boolean values from a Bernoulli distribution.
/// </summary>
/// <remarks>
/// A sample is <c>true</c> with the configured probability and <c>false</c> otherwise.
/// The sampler retains the supplied mutable generator and is not safe for concurrent use
/// without external synchronization.
/// </remarks>
public class BernoulliSampler : IBernoulliSampler
{
    IRandomSource generator;
    double probability;

    /// <summary>
    /// Creates a Bernoulli sampler with the requested success probability.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits.</param>
    /// <param name="probability">The finite probability of <c>true</c>, from zero through one inclusive.</param>
    /// <remarks>Probabilities zero and one define deterministic results that consume no random state.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, or the probability is non-finite or outside zero through one.</exception>
    public BernoulliSampler(IRandomSource generator, double probability)
    {
        Guard.NotNull(generator, "generator");
        Guard.Probability(probability, "probability");
        this.generator = generator;
        this.probability = probability;
    }

    /// <summary>
    /// Returns one value from this sampler's Bernoulli distribution.
    /// </summary>
    /// <returns><c>true</c> with the configured probability; otherwise, <c>false</c>.</returns>
    public bool Sample()
    {
        return ProbabilityUniform.NextBoolean(generator, probability);
    }

    /// <summary>
    /// Fills a destination with values from this sampler's Bernoulli distribution.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="Sample()"/>.</remarks>
    public void Fill(Span<bool> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample();
            index++;
        }
    }
}
