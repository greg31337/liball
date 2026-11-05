using System;

namespace random;

/// <summary>
/// Contains immutable guide-table preprocessing for repeated categorical index sampling.
/// </summary>
/// <remarks>
/// The guide narrows each cumulative search to the first category that can intersect an
/// equal-width unit interval. Private cumulative and guide arrays are derived defensively
/// from the supplied relative probabilities. The completed table can be shared across generators.
/// </remarks>
public class GuideTable : IGuideTable
{
    double[] cumulativeProbabilities;
    int[] guide;
    int deterministicIndex;

    /// <summary>
    /// Creates a guide table from finite nonnegative relative probabilities.
    /// </summary>
    /// <param name="probabilities">The nonempty finite nonnegative relative probabilities.</param>
    /// <remarks>
    /// The probabilities need not sum to one. Explicit zero entries are never selected,
    /// and later mutations of the input array do not affect this table.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The probability array is null or empty, an entry is non-finite or negative, or every entry is zero.</exception>
    public GuideTable(double[] probabilities)
    {
        CategoricalProbabilityTable table = new CategoricalProbabilityTable(probabilities, "probabilities");
        int count = table.Length();
        cumulativeProbabilities = new double[count];
        guide = new int[count];
        deterministicIndex = table.DeterministicIndex();
        int index = 0;
        while (index < count)
        {
            cumulativeProbabilities[index] = table.CumulativeProbability(index);
            index++;
        }

        int category = 0;
        index = 0;
        while (index < count)
        {
            double boundary = (double)index / (double)count;
            while (category < count - 1 && boundary >= cumulativeProbabilities[category])
            {
                category++;
            }

            guide[index] = category;
            index++;
        }
    }

    /// <summary>
    /// Returns the number of category indexes represented by this table.
    /// </summary>
    /// <returns>The positive category count.</returns>
    public int CategoryCount()
    {
        return cumulativeProbabilities.Length;
    }

    /// <summary>
    /// Samples one category index using this guide table.
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

        double unit = FloatingUniform.NextDoubleClosedOpen(generator);
        int guideIndex = (int)(unit * (double)guide.Length);
        if (guideIndex >= guide.Length)
        {
            guideIndex = guide.Length - 1;
        }

        int category = guide[guideIndex];
        while (category > 0 && unit < cumulativeProbabilities[category - 1])
        {
            category--;
        }

        while (category < cumulativeProbabilities.Length - 1 && unit >= cumulativeProbabilities[category])
        {
            category++;
        }

        return category;
    }

    /// <summary>
    /// Fills a destination with category indexes using this guide table.
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
