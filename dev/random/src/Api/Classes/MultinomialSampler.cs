using System;

namespace random;

/// <summary>
/// Generates category-count vectors from a multinomial distribution.
/// </summary>
/// <remarks>
/// Finite nonnegative relative probabilities are normalized during construction and
/// copied into private preprocessing. Sampling uses sequential conditional binomial
/// draws in category order, and the final category receives the exact remaining trial
/// count. The sampler retains the supplied mutable generator and is not safe for
/// concurrent use without external synchronization.
/// </remarks>
public class MultinomialSampler : IMultinomialSampler
{
    IRandomSource generator;
    double[] normalizedProbabilities;
    double[] tailProbabilities;
    int trials;

    /// <summary>
    /// Creates a multinomial sampler from a trial count and relative probabilities.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="trials">The nonnegative total number of trials.</param>
    /// <param name="probabilities">The nonempty finite nonnegative relative probabilities.</param>
    /// <remarks>
    /// The probabilities need not sum to one. They are defensively preprocessed, explicit
    /// zero entries receive zero counts, and one positive entry defines a deterministic vector.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator or probability array is null, trials are negative, the array is empty, an entry is non-finite or negative, or every entry is zero.</exception>
    public MultinomialSampler(IRandomSource generator, int trials, double[] probabilities)
    {
        Guard.NotNull(generator, "generator");
        Guard.Count(trials, "trials");
        CategoricalProbabilityTable table = new CategoricalProbabilityTable(probabilities, "probabilities");
        this.generator = generator;
        this.trials = trials;
        normalizedProbabilities = new double[table.Length()];
        tailProbabilities = new double[table.Length()];
        int index = 0;
        while (index < normalizedProbabilities.Length)
        {
            normalizedProbabilities[index] = table.NormalizedProbability(index);
            index++;
        }

        double tail = 0.0;
        double compensation = 0.0;
        index = normalizedProbabilities.Length - 1;
        while (index >= 0)
        {
            double probability = normalizedProbabilities[index];
            if (probability > 0.0)
            {
                double adjusted = probability - compensation;
                double next = tail + adjusted;
                compensation = (next - tail) - adjusted;
                tail = next;
            }

            tailProbabilities[index] = tail;
            index--;
        }
    }

    /// <summary>
    /// Returns the number of categories represented by each sampled vector.
    /// </summary>
    /// <returns>The positive category count.</returns>
    public int CategoryCount()
    {
        return normalizedProbabilities.Length;
    }

    /// <summary>
    /// Returns the exact storage length required for a row-major batch.
    /// </summary>
    /// <param name="sampleCount">The nonnegative number of category-count rows.</param>
    /// <returns>The product of the sample count and <see cref="CategoryCount()"/>.</returns>
    /// <exception cref="RandomArgumentException">The sample count is negative or the required length exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredBatchOutputLength(int sampleCount)
    {
        return MultivariateDistributions.RequiredVectorOutputLength(sampleCount, normalizedProbabilities.Length);
    }

    /// <summary>
    /// Creates one category-count vector from this multinomial distribution.
    /// </summary>
    /// <returns>A new vector whose entries sum exactly to the configured trial count.</returns>
    public int[] Sample()
    {
        int[] result = new int[normalizedProbabilities.Length];
        SampleInto(result.AsSpan());
        return result;
    }

    /// <summary>
    /// Fills a row-major batch with category-count vectors from this multinomial distribution.
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
            SampleInto(destination.Slice(offset, normalizedProbabilities.Length));
            offset += normalizedProbabilities.Length;
            row++;
        }
    }

    /// <summary>
    /// Writes one category-count vector from this multinomial distribution.
    /// </summary>
    /// <param name="destination">The destination, whose length must equal <see cref="CategoryCount()"/>.</param>
    /// <remarks>
    /// Every entry is nonnegative, explicit zero-probability categories receive zero,
    /// and the entries sum exactly to the configured trial count.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The destination length does not equal <see cref="CategoryCount()"/>.</exception>
    public void SampleInto(Span<int> destination)
    {
        Guard.ExactSpanLength(destination.Length, normalizedProbabilities.Length, "destination");
        int remainingTrials = trials;
        int index = 0;
        while (index < normalizedProbabilities.Length - 1)
        {
            int selected;
            double probability = normalizedProbabilities[index];
            if (remainingTrials == 0 || probability == 0.0)
            {
                selected = 0;
            }
            else if (tailProbabilities[index] <= probability)
            {
                selected = remainingTrials;
            }
            else
            {
                double conditionalProbability = probability / tailProbabilities[index];
                if (conditionalProbability > 1.0)
                {
                    conditionalProbability = 1.0;
                }

                BinomialSampler conditional = new BinomialSampler(
                    generator,
                    remainingTrials,
                    conditionalProbability);
                selected = conditional.Sample();
            }

            destination[index] = selected;
            remainingTrials -= selected;
            index++;
        }

        destination[normalizedProbabilities.Length - 1] = remainingTrials;
    }
}
