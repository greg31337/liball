using System;

namespace random;

/// <summary>
/// Generates signed 32-bit counts from a beta-binomial distribution.
/// </summary>
/// <remarks>
/// Each sample first draws a success probability from a beta distribution and then
/// draws a binomial count conditionally on that probability. Trial counts use the same
/// nonnegative signed 32-bit domain as <see cref="BinomialSampler"/>, including
/// <c>int.MaxValue</c>, without forming a wider or overflowing intermediate count.
/// The sampler retains the supplied mutable generator and is not safe for concurrent
/// use without external synchronization.
/// </remarks>
public class BetaBinomialSampler : IBetaBinomialSampler
{
    IRandomSource generator;
    BetaSampler betaSampler;
    int trials;

    /// <summary>
    /// Creates a beta-binomial sampler with the requested trial count and beta shapes.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="trials">The nonnegative number of conditional binomial trials.</param>
    /// <param name="alpha">The finite positive first beta shape.</param>
    /// <param name="beta">The finite positive second beta shape.</param>
    /// <remarks>Zero trials define a point mass at zero that consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the trial count is negative, or either shape is non-finite or not positive.</exception>
    public BetaBinomialSampler(IRandomSource generator, int trials, double alpha, double beta)
    {
        Guard.NotNull(generator, "generator");
        Guard.Count(trials, "trials");
        BetaSampler.ValidateParameters(generator, alpha, beta);
        BetaSampler validatedBetaSampler = new BetaSampler(generator, alpha, beta);
        this.generator = generator;
        betaSampler = validatedBetaSampler;
        this.trials = trials;
    }

    /// <summary>
    /// Returns one count from this sampler's beta-binomial distribution.
    /// </summary>
    /// <returns>An integer from zero through the configured trial count inclusive.</returns>
    public int Sample()
    {
        if (trials == 0)
        {
            return 0;
        }

        double probability = betaSampler.Sample();
        BinomialSampler conditional = new BinomialSampler(generator, trials, probability);
        return conditional.Sample();
    }

    /// <summary>
    /// Fills a destination with counts from this sampler's beta-binomial distribution.
    /// </summary>
    /// <param name="destination">The destination counts.</param>
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
