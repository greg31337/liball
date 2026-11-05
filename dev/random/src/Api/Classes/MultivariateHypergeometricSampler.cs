using System;

namespace random;

/// <summary>
/// Generates category-count vectors from a multivariate hypergeometric distribution.
/// </summary>
/// <remarks>
/// Sampling draws a fixed total without replacement from disjoint categories. Categories
/// are processed in input order through conditional univariate hypergeometric draws, and
/// the final category receives the exact remaining draw count. The category array is copied
/// during construction. The sampler retains the supplied mutable generator and is not safe
/// for concurrent use without external synchronization.
/// </remarks>
public class MultivariateHypergeometricSampler : IMultivariateHypergeometricSampler
{
    IRandomSource generator;
    int[] categoryCounts;
    int populationSize;
    int draws;

    /// <summary>
    /// Creates a multivariate hypergeometric sampler.
    /// </summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="categoryCounts">The nonempty array of nonnegative category population counts.</param>
    /// <param name="draws">The number of members drawn without replacement.</param>
    /// <remarks>
    /// The category counts are copied. Their total must fit a signed 32-bit population,
    /// and draws must be between zero and that total inclusive.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator or category array is null, the array is empty, a category count is negative, the population total exceeds <c>int.MaxValue</c>, or draws are outside the population.</exception>
    public MultivariateHypergeometricSampler(IRandomSource generator, int[] categoryCounts, int draws)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(categoryCounts, "categoryCounts");
        if (categoryCounts.Length == 0)
        {
            throw RandomArgumentException.Create("categoryCounts", "At least one category count is required.");
        }

        long total = 0L;
        int index = 0;
        while (index < categoryCounts.Length)
        {
            int count = categoryCounts[index];
            if (count < 0)
            {
                throw RandomArgumentException.Create("categoryCounts", "Every category count must be nonnegative.");
            }

            total += (long)count;
            if (total > (long)int.MaxValue)
            {
                throw RandomArgumentException.Create("categoryCounts", "The total population must fit a signed 32-bit count.");
            }

            index++;
        }

        Guard.Count(draws, "draws");
        if ((long)draws > total)
        {
            throw RandomArgumentException.Create("draws", "The draw count must not exceed the total population.");
        }

        this.generator = generator;
        this.categoryCounts = (int[])categoryCounts.Clone();
        populationSize = (int)total;
        this.draws = draws;
    }

    /// <summary>
    /// Returns the number of categories represented by each sampled vector.
    /// </summary>
    /// <returns>The positive category count.</returns>
    public int CategoryCount()
    {
        return categoryCounts.Length;
    }

    /// <summary>
    /// Returns the exact storage length required for a row-major batch.
    /// </summary>
    /// <param name="sampleCount">The nonnegative number of category-count rows.</param>
    /// <returns>The product of the sample count and <see cref="CategoryCount()"/>.</returns>
    /// <exception cref="RandomArgumentException">The sample count is negative or the required length exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredBatchOutputLength(int sampleCount)
    {
        return MultivariateDistributions.RequiredVectorOutputLength(sampleCount, categoryCounts.Length);
    }

    /// <summary>
    /// Creates one category-count vector from this multivariate hypergeometric distribution.
    /// </summary>
    /// <returns>A new vector whose entries sum exactly to the configured draw count.</returns>
    public int[] Sample()
    {
        int[] result = new int[categoryCounts.Length];
        SampleInto(result.AsSpan());
        return result;
    }

    /// <summary>
    /// Fills a row-major batch with category-count vectors from this multivariate hypergeometric distribution.
    /// </summary>
    /// <param name="sampleCount">The nonnegative number of category-count rows.</param>
    /// <param name="destination">The destination, whose length must equal <see cref="RequiredBatchOutputLength(int)"/> for the sample count.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="SampleInto(Span{int})"/> in increasing row order.</remarks>
    /// <exception cref="RandomArgumentException">The sample count is invalid or the destination length does not match the required row-major storage.</exception>
    public void Fill(int sampleCount, Span<int> destination)
    {
        int requiredLength = RequiredBatchOutputLength(sampleCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int row = 0;
        int offset = 0;
        while (row < sampleCount)
        {
            SampleInto(destination.Slice(offset, categoryCounts.Length));
            offset += categoryCounts.Length;
            row++;
        }
    }

    /// <summary>
    /// Writes one category-count vector from this multivariate hypergeometric distribution.
    /// </summary>
    /// <param name="destination">The destination, whose length must equal <see cref="CategoryCount()"/>.</param>
    /// <remarks>
    /// Every entry is between zero and its corresponding population count inclusive,
    /// and the entries sum exactly to the configured draw count.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The destination length does not equal <see cref="CategoryCount()"/>.</exception>
    public void SampleInto(Span<int> destination)
    {
        Guard.ExactSpanLength(destination.Length, categoryCounts.Length, "destination");
        int remainingPopulation = populationSize;
        int remainingDraws = draws;
        int index = 0;
        while (index < categoryCounts.Length - 1)
        {
            int selected;
            if (remainingDraws == 0 || categoryCounts[index] == 0)
            {
                selected = 0;
            }
            else if (remainingDraws == remainingPopulation)
            {
                selected = categoryCounts[index];
            }
            else
            {
                HypergeometricSampler conditional = new HypergeometricSampler(
                    generator,
                    remainingPopulation,
                    categoryCounts[index],
                    remainingDraws);
                selected = conditional.Sample();
            }

            destination[index] = selected;
            remainingDraws -= selected;
            remainingPopulation -= categoryCounts[index];
            index++;
        }

        destination[categoryCounts.Length - 1] = remainingDraws;
    }
}
