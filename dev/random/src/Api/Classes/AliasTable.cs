using System;

namespace random;

/// <summary>
/// Contains immutable alias-method preprocessing for repeated categorical index sampling.
/// </summary>
/// <remarks>
/// Construction uses Vose's alias method and defensively derives private threshold and
/// alias arrays from the supplied relative probabilities. The completed table can be
/// shared across generators; callers remain responsible for each generator's thread ownership.
/// </remarks>
public class AliasTable : IAliasTable
{
    double[] thresholds;
    int[] aliases;
    int deterministicIndex;

    /// <summary>
    /// Creates an alias table from finite nonnegative relative probabilities.
    /// </summary>
    /// <param name="probabilities">The nonempty finite nonnegative relative probabilities.</param>
    /// <remarks>
    /// The probabilities need not sum to one. Explicit zero entries are never selected,
    /// and later mutations of the input array do not affect this table.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The probability array is null or empty, an entry is non-finite or negative, or every entry is zero.</exception>
    public AliasTable(double[] probabilities)
    {
        CategoricalProbabilityTable table = new CategoricalProbabilityTable(probabilities, "probabilities");
        int count = table.Length();
        thresholds = new double[count];
        aliases = new int[count];
        deterministicIndex = table.DeterministicIndex();
        if (deterministicIndex >= 0)
        {
            int deterministicFillIndex = 0;
            while (deterministicFillIndex < count)
            {
                aliases[deterministicFillIndex] = deterministicIndex;
                deterministicFillIndex++;
            }

            return;
        }

        double[] scaledProbabilities = new double[count];
        int[] small = new int[count];
        int[] large = new int[count];
        int smallCount = 0;
        int largeCount = 0;
        int index = 0;
        while (index < count)
        {
            double scaled = table.NormalizedProbability(index) * (double)count;
            scaledProbabilities[index] = scaled;
            aliases[index] = index;
            if (scaled < 1.0)
            {
                small[smallCount] = index;
                smallCount++;
            }
            else
            {
                large[largeCount] = index;
                largeCount++;
            }

            index++;
        }

        while (smallCount > 0 && largeCount > 0)
        {
            smallCount--;
            int smallIndex = small[smallCount];
            largeCount--;
            int largeIndex = large[largeCount];
            thresholds[smallIndex] = scaledProbabilities[smallIndex];
            aliases[smallIndex] = largeIndex;
            double replacement = scaledProbabilities[largeIndex] + scaledProbabilities[smallIndex] - 1.0;
            scaledProbabilities[largeIndex] = replacement;
            if (replacement < 1.0)
            {
                small[smallCount] = largeIndex;
                smallCount++;
            }
            else
            {
                large[largeCount] = largeIndex;
                largeCount++;
            }
        }

        while (largeCount > 0)
        {
            largeCount--;
            int largeIndex = large[largeCount];
            thresholds[largeIndex] = 1.0;
            aliases[largeIndex] = largeIndex;
        }

        while (smallCount > 0)
        {
            smallCount--;
            int smallIndex = small[smallCount];
            thresholds[smallIndex] = 1.0;
            aliases[smallIndex] = smallIndex;
        }
    }

    /// <summary>
    /// Returns the number of category indexes represented by this table.
    /// </summary>
    /// <returns>The positive category count.</returns>
    public int CategoryCount()
    {
        return thresholds.Length;
    }

    /// <summary>
    /// Samples one category index using this alias table.
    /// </summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <returns>An index from zero through <see cref="CategoryCount()"/> minus one.</returns>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public int Sample(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        if (deterministicIndex >= 0)
        {
            return deterministicIndex;
        }

        int column = IntegerUniform.NextInt32(generator, thresholds.Length);
        double threshold = thresholds[column];
        if (threshold <= 0.0)
        {
            return aliases[column];
        }

        if (threshold >= 1.0)
        {
            return column;
        }

        if (FloatingUniform.NextDoubleClosedOpen(generator) < threshold)
        {
            return column;
        }

        return aliases[column];
    }

    /// <summary>
    /// Fills a destination with category indexes using this alias table.
    /// </summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="destination">The destination indexes.</param>
    /// <remarks>The result and source state match repeated calls to <see cref="Sample(IRandomSource)"/>.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public void Fill(IRandomSource generator, Span<int> destination)
    {
        Guard.NotNull(generator, "generator");
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample(generator);
            index++;
        }
    }
}
