using System;
using System.Collections;

namespace random;

/// <summary>
/// Provides weighted selection, bootstrap, and particle-resampling operations.
/// </summary>
/// <remarks>
/// Enumerated sources are materialized exactly once before random state is consumed.
/// Weighted draws can use cumulative, guide-table, or alias-table preprocessing.
/// Sampling without replacement uses exponential keys. This class retains the supplied
/// generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class WeightedCollectionSampler : IWeightedCollectionSampler
{
    IRandomSource generator;

    /// <summary>
    /// Creates a weighted collection sampler backed by a random source.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and indexes.</param>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public WeightedCollectionSampler(IRandomSource generator)
    {
        Guard.NotNull(generator, "generator");
        this.generator = generator;
    }

    /// <summary>Returns one category index selected according to relative weights.</summary>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="method">The preprocessing and draw algorithm.</param>
    /// <returns>The selected zero-based category index.</returns>
    /// <exception cref="RandomArgumentException">The weights or method are invalid.</exception>
    public int WeightedIndex(double[] weights, WeightedSamplingMethod method)
    {
        WeightedSampler sampler = WeightedSampler.FromWeights(generator, weights, method);
        return sampler.SampleIndex();
    }

    /// <summary>Returns category indexes sampled with replacement according to relative weights.</summary>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <param name="method">The preprocessing and draw algorithm.</param>
    /// <returns>The selected zero-based category indexes.</returns>
    /// <exception cref="RandomArgumentException">The weights, count, or method are invalid.</exception>
    public int[] WeightedIndexes(double[] weights, int count, WeightedSamplingMethod method)
    {
        Guard.Count(count, "count");
        WeightedSampler sampler = WeightedSampler.FromWeights(generator, weights, method);
        int[] result = new int[count];
        sampler.FillIndices(result.AsSpan());
        return result;
    }

    /// <summary>Fills a destination with category indexes sampled with replacement.</summary>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="method">The preprocessing and draw algorithm.</param>
    /// <param name="destination">The destination indexes.</param>
    /// <exception cref="RandomArgumentException">The weights or method are invalid.</exception>
    public void FillWeightedIndexes(double[] weights, WeightedSamplingMethod method, Span<int> destination)
    {
        WeightedSampler sampler = WeightedSampler.FromWeights(generator, weights, method);
        sampler.FillIndices(destination);
    }

    /// <summary>Returns one enumerated value selected according to relative weights.</summary>
    /// <param name="values">The source values, in weight order.</param>
    /// <param name="weights">One relative weight per source value.</param>
    /// <param name="method">The preprocessing and draw algorithm.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">An input, weight, or method is invalid.</exception>
    public object? WeightedValue(IEnumerable values, double[] weights, WeightedSamplingMethod method)
    {
        object?[] materialized = MaterializeWeightedValues(values, weights);
        int index = WeightedIndex(weights, method);
        return materialized[index];
    }

    /// <summary>Returns enumerated values sampled with replacement according to relative weights.</summary>
    /// <param name="values">The source values, in weight order.</param>
    /// <param name="weights">One relative weight per source value.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <param name="method">The preprocessing and draw algorithm.</param>
    /// <returns>The selected values.</returns>
    /// <exception cref="RandomArgumentException">An input, weight, count, or method is invalid.</exception>
    public object?[] WeightedValues(IEnumerable values, double[] weights, int count, WeightedSamplingMethod method)
    {
        Guard.Count(count, "count");
        object?[] materialized = MaterializeWeightedValues(values, weights);
        int[] indexes = WeightedIndexes(weights, count, method);
        return SelectValues(materialized, indexes);
    }

    /// <summary>Returns an ordered weighted sample of distinct category indexes.</summary>
    /// <param name="weights">The nonnegative relative weights, which may include positive infinity.</param>
    /// <param name="count">The number of distinct indexes to sample.</param>
    /// <returns>The selected indexes in exponential-race order.</returns>
    /// <exception cref="RandomArgumentException">A weight or count is invalid or too few weights are positive.</exception>
    public int[] WeightedIndexesWithoutReplacement(double[] weights, int count)
    {
        return RandomCollections.WeightedSampleWithoutReplacement(generator, weights, count);
    }

    /// <summary>Fills a destination with an ordered weighted sample of distinct indexes.</summary>
    /// <param name="weights">The nonnegative relative weights, which may include positive infinity.</param>
    /// <param name="destination">The destination whose length determines the sample count.</param>
    /// <exception cref="RandomArgumentException">A weight is invalid or too few weights are positive.</exception>
    public void FillWeightedIndexesWithoutReplacement(double[] weights, Span<int> destination)
    {
        RandomCollections.FillWeightedSampleWithoutReplacement(generator, weights, destination);
    }

    /// <summary>Returns an ordered weighted sample of distinct enumerated values.</summary>
    /// <param name="values">The source values, in weight order.</param>
    /// <param name="weights">One relative weight per source value.</param>
    /// <param name="count">The number of distinct values to sample by source position.</param>
    /// <returns>The selected values in exponential-race order.</returns>
    /// <exception cref="RandomArgumentException">An input, weight, or count is invalid.</exception>
    public object?[] WeightedValuesWithoutReplacement(IEnumerable values, double[] weights, int count)
    {
        object?[] materialized = MaterializeWeightedValues(values, weights);
        int[] indexes = WeightedIndexesWithoutReplacement(weights, count);
        return SelectValues(materialized, indexes);
    }

    /// <summary>Returns uniformly sampled bootstrap indexes.</summary>
    /// <param name="populationSize">The positive exclusive upper index bound.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>The sampled indexes.</returns>
    /// <exception cref="RandomArgumentException">The population size or count is invalid.</exception>
    public int[] BootstrapIndexes(int populationSize, int count)
    {
        return RandomCollections.BootstrapIndexes(generator, populationSize, count);
    }

    /// <summary>Fills a destination with uniformly sampled bootstrap indexes.</summary>
    /// <param name="populationSize">The positive exclusive upper index bound.</param>
    /// <param name="destination">The destination indexes.</param>
    /// <exception cref="RandomArgumentException">The population size is invalid.</exception>
    public void FillBootstrapIndexes(int populationSize, Span<int> destination)
    {
        RandomCollections.FillBootstrapIndexes(generator, populationSize, destination);
    }

    /// <summary>Returns a bootstrap sample from a one-dimensional array.</summary>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>A zero-based array whose runtime element type matches the source.</returns>
    /// <exception cref="RandomArgumentException">The source or count is invalid.</exception>
    public Array BootstrapArray(Array values, int count)
    {
        return RandomCollections.BootstrapSample(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample from a list.</summary>
    /// <param name="values">The nonempty source list.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>The sampled list values.</returns>
    /// <exception cref="RandomArgumentException">The source or count is invalid.</exception>
    public object?[] BootstrapList(IList values, int count)
    {
        return RandomCollections.BootstrapSample(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample from an enumerated source.</summary>
    /// <param name="values">The nonempty source sequence.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>The sampled source values.</returns>
    /// <remarks>The source is fully materialized before any index is sampled.</remarks>
    /// <exception cref="RandomArgumentException">The source or count is invalid.</exception>
    public object?[] BootstrapValues(IEnumerable values, int count)
    {
        Guard.Count(count, "count");
        object?[] materialized = MaterializeValues(values);
        Guard.Argument(materialized.Length > 0, "values", "At least one source value is required.");
        int[] indexes = BootstrapIndexes(materialized.Length, count);
        return SelectValues(materialized, indexes);
    }

    /// <summary>Returns stratified-resampling ancestor indexes.</summary>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>The ancestor indexes.</returns>
    /// <exception cref="RandomArgumentException">A weight or count is invalid.</exception>
    public int[] StratifiedIndexes(double[] weights, int count)
    {
        return RandomCollections.StratifiedResample(generator, weights, count);
    }

    /// <summary>Fills a destination with stratified-resampling ancestor indexes.</summary>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="destination">The destination ancestor indexes.</param>
    /// <exception cref="RandomArgumentException">A weight is invalid.</exception>
    public void FillStratifiedIndexes(double[] weights, Span<int> destination)
    {
        RandomCollections.FillStratifiedResample(generator, weights, destination);
    }

    /// <summary>Returns stratified-resampled values from an enumerated source.</summary>
    /// <param name="values">The source values, in weight order.</param>
    /// <param name="weights">One relative weight per source value.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>The resampled values.</returns>
    /// <exception cref="RandomArgumentException">An input, weight, or count is invalid.</exception>
    public object?[] StratifiedValues(IEnumerable values, double[] weights, int count)
    {
        object?[] materialized = MaterializeWeightedValues(values, weights);
        int[] indexes = StratifiedIndexes(weights, count);
        return SelectValues(materialized, indexes);
    }

    /// <summary>Returns systematic-resampling ancestor indexes.</summary>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>The ancestor indexes.</returns>
    /// <exception cref="RandomArgumentException">A weight or count is invalid.</exception>
    public int[] SystematicIndexes(double[] weights, int count)
    {
        return RandomCollections.SystematicResample(generator, weights, count);
    }

    /// <summary>Fills a destination with systematic-resampling ancestor indexes.</summary>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="destination">The destination ancestor indexes.</param>
    /// <exception cref="RandomArgumentException">A weight is invalid.</exception>
    public void FillSystematicIndexes(double[] weights, Span<int> destination)
    {
        RandomCollections.FillSystematicResample(generator, weights, destination);
    }

    /// <summary>Returns systematic-resampled values from an enumerated source.</summary>
    /// <param name="values">The source values, in weight order.</param>
    /// <param name="weights">One relative weight per source value.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>The resampled values.</returns>
    /// <exception cref="RandomArgumentException">An input, weight, or count is invalid.</exception>
    public object?[] SystematicValues(IEnumerable values, double[] weights, int count)
    {
        object?[] materialized = MaterializeWeightedValues(values, weights);
        int[] indexes = SystematicIndexes(weights, count);
        return SelectValues(materialized, indexes);
    }

    /// <summary>Returns residual-resampling ancestor indexes.</summary>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>The ancestor indexes.</returns>
    /// <exception cref="RandomArgumentException">A weight or count is invalid.</exception>
    public int[] ResidualIndexes(double[] weights, int count)
    {
        return RandomCollections.ResidualResample(generator, weights, count);
    }

    /// <summary>Fills a destination with residual-resampling ancestor indexes.</summary>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="destination">The destination ancestor indexes.</param>
    /// <exception cref="RandomArgumentException">A weight is invalid.</exception>
    public void FillResidualIndexes(double[] weights, Span<int> destination)
    {
        RandomCollections.FillResidualResample(generator, weights, destination);
    }

    /// <summary>Returns residual-resampled values from an enumerated source.</summary>
    /// <param name="values">The source values, in weight order.</param>
    /// <param name="weights">One relative weight per source value.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>The resampled values.</returns>
    /// <exception cref="RandomArgumentException">An input, weight, or count is invalid.</exception>
    public object?[] ResidualValues(IEnumerable values, double[] weights, int count)
    {
        object?[] materialized = MaterializeWeightedValues(values, weights);
        int[] indexes = ResidualIndexes(weights, count);
        return SelectValues(materialized, indexes);
    }

    static object?[] MaterializeWeightedValues(IEnumerable values, double[] weights)
    {
        Guard.NotNull(weights, "weights");
        object?[] materialized = MaterializeValues(values);
        Guard.Argument(materialized.Length == weights.Length, "weights", "There must be one weight for each source value.");
        return materialized;
    }

    static object?[] MaterializeValues(IEnumerable values)
    {
        Guard.NotNull(values, "values");
        ArrayList collected = new ArrayList();
        foreach (object? value in values)
        {
            collected.Add(value);
        }

        object?[] result = new object?[collected.Count];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = collected[index];
            index++;
        }

        return result;
    }

    static object?[] SelectValues(object?[] values, int[] indexes)
    {
        object?[] result = new object?[indexes.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = values[indexes[index]];
            index++;
        }

        return result;
    }
}
