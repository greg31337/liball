using System;

namespace random;

/// <summary>
/// Generates category indexes from finite nonnegative relative probabilities.
/// </summary>
/// <remarks>
/// The constructor normalizes the supplied relative probabilities into a cumulative
/// table. The input array is not retained, and later caller mutations cannot affect
/// the sampler. Categories are indexed from zero in input order. The sampler retains
/// the supplied mutable generator and is not safe for concurrent use without external
/// synchronization.
/// </remarks>
public class CategoricalSampler : ICategoricalSampler
{
    IRandomSource generator;
    double[] cumulativeProbabilities;
    int deterministicIndex;

    /// <summary>
    /// Creates a categorical sampler from relative probabilities.
    /// </summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="probabilities">The nonempty finite nonnegative relative probabilities.</param>
    /// <remarks>
    /// The probabilities need not sum to one. Explicit zero entries are never selected.
    /// If exactly one entry is positive, sampling returns its index without consuming
    /// random state.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator or probability array is null, the array is empty, an entry is non-finite or negative, or every entry is zero.</exception>
    public CategoricalSampler(IRandomSource generator, double[] probabilities)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(probabilities, "probabilities");
        if (probabilities.Length == 0)
        {
            throw RandomArgumentException.Create("probabilities", "At least one probability is required.");
        }

        double maximum = 0.0;
        int positiveCount = 0;
        int positiveIndex = -1;
        int index = 0;
        while (index < probabilities.Length)
        {
            double probability = probabilities[index];
            if (!double.IsFinite(probability) || probability < 0.0)
            {
                throw RandomArgumentException.Create("probabilities", "Every probability must be finite and nonnegative.");
            }

            if (probability > 0.0)
            {
                positiveCount++;
                positiveIndex = index;
                if (probability > maximum)
                {
                    maximum = probability;
                }
            }

            index++;
        }

        if (positiveCount == 0)
        {
            throw RandomArgumentException.Create("probabilities", "At least one probability must be positive.");
        }

        this.generator = generator;
        deterministicIndex = -1;
        cumulativeProbabilities = new double[probabilities.Length];
        if (positiveCount == 1)
        {
            deterministicIndex = positiveIndex;
            index = positiveIndex;
            while (index < cumulativeProbabilities.Length)
            {
                cumulativeProbabilities[index] = 1.0;
                index++;
            }

            return;
        }

        double total = 0.0;
        double compensation = 0.0;
        index = 0;
        while (index < probabilities.Length)
        {
            if (probabilities[index] > 0.0)
            {
                double scaled = probabilities[index] / maximum;
                double adjusted = scaled - compensation;
                double next = total + adjusted;
                compensation = (next - total) - adjusted;
                total = next;
            }

            index++;
        }

        double running = 0.0;
        compensation = 0.0;
        index = 0;
        while (index < probabilities.Length)
        {
            if (probabilities[index] > 0.0)
            {
                double scaled = probabilities[index] / maximum;
                double adjusted = scaled - compensation;
                double next = running + adjusted;
                compensation = (next - running) - adjusted;
                running = next;
            }

            cumulativeProbabilities[index] = running / total;
            index++;
        }

        index = positiveIndex;
        while (index < cumulativeProbabilities.Length)
        {
            cumulativeProbabilities[index] = 1.0;
            index++;
        }
    }

    /// <summary>
    /// Returns one category index from this sampler's categorical distribution.
    /// </summary>
    /// <returns>An index from zero through the probability-array length minus one.</returns>
    public int Sample()
    {
        if (deterministicIndex >= 0)
        {
            return deterministicIndex;
        }

        double unit = FloatingUniform.NextDoubleClosedOpen(generator);
        int index = 0;
        while (index < cumulativeProbabilities.Length)
        {
            if (unit < cumulativeProbabilities[index])
            {
                return index;
            }

            index++;
        }

        return cumulativeProbabilities.Length - 1;
    }

    /// <summary>
    /// Fills a destination with category indexes from this sampler's categorical distribution.
    /// </summary>
    /// <param name="destination">The destination indexes.</param>
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
