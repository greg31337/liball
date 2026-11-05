using System;

namespace random;

/// <summary>
/// Repeatedly samples weighted category indexes with replacement using reusable preprocessing.
/// </summary>
/// <remarks>
/// The sampler owns a generator reference and a defensive preprocessing snapshot. Instances are
/// mutable through <see cref="UpdateWeights(double[])"/> and require external synchronization
/// when shared between callers.
/// </remarks>
public class WeightedSampler : IWeightedSampler
{
    IRandomSource generator;
    WeightedSamplingMethod method;
    double[] cumulativeProbabilities;
    int deterministicIndex;
    GuideTable? guideTable;
    AliasTable? aliasTable;
    int categoryCount;

    WeightedSampler(IRandomSource generator, WeightedSamplingMethod method)
    {
        this.generator = generator;
        this.method = method;
        cumulativeProbabilities = new double[0];
        deterministicIndex = -1;
    }

    /// <summary>Creates a cumulative-table weighted sampler.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <returns>A cumulative weighted sampler with a defensive weight snapshot.</returns>
    /// <exception cref="RandomArgumentException">The generator or weights are null, the weights are empty or invalid, or every weight is zero.</exception>
    internal static WeightedSampler FromWeights(IRandomSource generator, double[] weights)
    {
        return FromWeights(generator, weights, WeightedSamplingMethod.Cumulative);
    }

    /// <summary>Creates a weighted sampler using the requested preprocessing method.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="method">The cumulative, guide-table, or alias-table method.</param>
    /// <returns>A weighted sampler with a defensive weight snapshot.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the method is unknown, the weights are empty or invalid, or every weight is zero.</exception>
    internal static WeightedSampler FromWeights(IRandomSource generator, double[] weights, WeightedSamplingMethod method)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(weights, "weights");
        ValidateMethod(method);
        WeightedSampler result = new WeightedSampler(generator, method);
        result.ReplaceWeights(weights);
        return result;
    }

    /// <summary>Creates a guide-table weighted sampler from relative weights.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <returns>A guide-table weighted sampler.</returns>
    /// <exception cref="RandomArgumentException">The generator or weights are null, the weights are empty or invalid, or every weight is zero.</exception>
    internal static WeightedSampler FromGuideWeights(IRandomSource generator, double[] weights)
    {
        return FromWeights(generator, weights, WeightedSamplingMethod.GuideTable);
    }

    /// <summary>Creates an alias-table weighted sampler from relative weights.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <returns>An alias-table weighted sampler.</returns>
    /// <exception cref="RandomArgumentException">The generator or weights are null, the weights are empty or invalid, or every weight is zero.</exception>
    internal static WeightedSampler FromAliasWeights(IRandomSource generator, double[] weights)
    {
        return FromWeights(generator, weights, WeightedSamplingMethod.AliasTable);
    }

    /// <summary>Creates a weighted sampler that reuses an immutable guide table.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="table">The preprocessed guide table.</param>
    /// <returns>A guide-table weighted sampler.</returns>
    /// <exception cref="RandomArgumentException">The generator or table is null.</exception>
    internal static WeightedSampler FromGuideTable(IRandomSource generator, GuideTable table)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(table, "table");
        WeightedSampler result = new WeightedSampler(generator, WeightedSamplingMethod.GuideTable);
        result.guideTable = table;
        result.categoryCount = table.CategoryCount();
        return result;
    }

    /// <summary>Creates a weighted sampler that reuses an immutable alias table.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="table">The preprocessed alias table.</param>
    /// <returns>An alias-table weighted sampler.</returns>
    /// <exception cref="RandomArgumentException">The generator or table is null.</exception>
    internal static WeightedSampler FromAliasTable(IRandomSource generator, AliasTable table)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(table, "table");
        WeightedSampler result = new WeightedSampler(generator, WeightedSamplingMethod.AliasTable);
        result.aliasTable = table;
        result.categoryCount = table.CategoryCount();
        return result;
    }

    /// <summary>Returns the number of weighted categories.</summary>
    /// <returns>The positive category count.</returns>
    public int CategoryCount()
    {
        return categoryCount;
    }

    /// <summary>Returns the configured weighted sampling method.</summary>
    /// <returns>The cumulative, guide-table, or alias-table method.</returns>
    public WeightedSamplingMethod Method()
    {
        return method;
    }

    /// <summary>Samples one category index with replacement.</summary>
    /// <returns>An index from zero through <see cref="CategoryCount()"/> minus one.</returns>
    public int SampleIndex()
    {
        switch (method)
        {
            case WeightedSamplingMethod.Cumulative:
                return SampleCumulative();
            case WeightedSamplingMethod.GuideTable:
                return GetGuideTable().Sample(generator);
            case WeightedSamplingMethod.AliasTable:
                return GetAliasTable().Sample(generator);
            default:
                throw RandomStateException.Create("The weighted sampling method is invalid.");
        }
    }

    /// <summary>Fills a destination with weighted category indexes sampled with replacement.</summary>
    /// <param name="destination">The destination indexes.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="SampleIndex()"/>.</remarks>
    public void FillIndices(Span<int> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = SampleIndex();
            index++;
        }
    }

    /// <summary>Atomically replaces the relative weights while retaining the generator and sampling method.</summary>
    /// <param name="weights">The nonempty finite nonnegative replacement weights.</param>
    /// <remarks>Validation and preprocessing consume no random state. Invalid weights leave the current sampler unchanged.</remarks>
    /// <exception cref="RandomArgumentException">The weights are null, empty or invalid, or every weight is zero.</exception>
    public void UpdateWeights(double[] weights)
    {
        Guard.NotNull(weights, "weights");
        ReplaceWeights(weights);
    }

    void ReplaceWeights(double[] weights)
    {
        CategoricalProbabilityTable validated = new CategoricalProbabilityTable(weights, "weights");
        if (method == WeightedSamplingMethod.Cumulative)
        {
            double[] replacement = new double[validated.Length()];
            int index = 0;
            while (index < replacement.Length)
            {
                replacement[index] = validated.CumulativeProbability(index);
                index++;
            }

            cumulativeProbabilities = replacement;
            deterministicIndex = validated.DeterministicIndex();
            guideTable = null;
            aliasTable = null;
            categoryCount = replacement.Length;
            return;
        }

        if (method == WeightedSamplingMethod.GuideTable)
        {
            GuideTable replacement = new GuideTable(weights);
            guideTable = replacement;
            aliasTable = null;
            cumulativeProbabilities = new double[0];
            deterministicIndex = -1;
            categoryCount = replacement.CategoryCount();
            return;
        }

        AliasTable aliasReplacement = new AliasTable(weights);
        aliasTable = aliasReplacement;
        guideTable = null;
        cumulativeProbabilities = new double[0];
        deterministicIndex = -1;
        categoryCount = aliasReplacement.CategoryCount();
    }

    int SampleCumulative()
    {
        if (deterministicIndex >= 0)
        {
            return deterministicIndex;
        }

        double unit = FloatingUniform.NextDoubleClosedOpen(generator);
        int index = 0;
        while (index < cumulativeProbabilities.Length - 1)
        {
            if (unit < cumulativeProbabilities[index])
            {
                return index;
            }

            index++;
        }

        return cumulativeProbabilities.Length - 1;
    }

    GuideTable GetGuideTable()
    {
        if (guideTable == null)
        {
            throw RandomStateException.Create("The weighted sampler has no guide table.");
        }

        return guideTable;
    }

    AliasTable GetAliasTable()
    {
        if (aliasTable == null)
        {
            throw RandomStateException.Create("The weighted sampler has no alias table.");
        }

        return aliasTable;
    }

    static void ValidateMethod(WeightedSamplingMethod method)
    {
        if (method != WeightedSamplingMethod.Cumulative && method != WeightedSamplingMethod.GuideTable && method != WeightedSamplingMethod.AliasTable)
        {
            throw RandomArgumentException.Create("method", "The weighted sampling method is unknown.");
        }
    }
}
