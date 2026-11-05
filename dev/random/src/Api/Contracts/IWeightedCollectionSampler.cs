using System;
using System.Collections;

namespace random;

/// <summary>
/// Defines weighted selection, bootstrap, and particle-resampling operations.
/// </summary>
public interface IWeightedCollectionSampler
{
    /// <summary>Returns one weighted category index.</summary>
    int WeightedIndex(double[] weights, WeightedSamplingMethod method);

    /// <summary>Returns weighted category indexes sampled with replacement.</summary>
    int[] WeightedIndexes(double[] weights, int count, WeightedSamplingMethod method);

    /// <summary>Fills a destination with weighted category indexes sampled with replacement.</summary>
    void FillWeightedIndexes(double[] weights, WeightedSamplingMethod method, Span<int> destination);

    /// <summary>Returns one weighted value from an enumerated source.</summary>
    object? WeightedValue(IEnumerable values, double[] weights, WeightedSamplingMethod method);

    /// <summary>Returns weighted values sampled with replacement from an enumerated source.</summary>
    object?[] WeightedValues(IEnumerable values, double[] weights, int count, WeightedSamplingMethod method);

    /// <summary>Returns weighted category indexes sampled without replacement.</summary>
    int[] WeightedIndexesWithoutReplacement(double[] weights, int count);

    /// <summary>Fills a destination with weighted category indexes sampled without replacement.</summary>
    void FillWeightedIndexesWithoutReplacement(double[] weights, Span<int> destination);

    /// <summary>Returns weighted values sampled without replacement from an enumerated source.</summary>
    object?[] WeightedValuesWithoutReplacement(IEnumerable values, double[] weights, int count);

    /// <summary>Returns uniform bootstrap indexes.</summary>
    int[] BootstrapIndexes(int populationSize, int count);

    /// <summary>Fills a destination with uniform bootstrap indexes.</summary>
    void FillBootstrapIndexes(int populationSize, Span<int> destination);

    /// <summary>Returns a bootstrap sample retaining an array's runtime type.</summary>
    Array BootstrapArray(Array values, int count);

    /// <summary>Returns a bootstrap sample from a list.</summary>
    object?[] BootstrapList(IList values, int count);

    /// <summary>Returns a bootstrap sample from an enumerated source.</summary>
    object?[] BootstrapValues(IEnumerable values, int count);

    /// <summary>Returns stratified-resampling ancestor indexes.</summary>
    int[] StratifiedIndexes(double[] weights, int count);

    /// <summary>Fills a destination with stratified-resampling ancestor indexes.</summary>
    void FillStratifiedIndexes(double[] weights, Span<int> destination);

    /// <summary>Returns stratified-resampled values from an enumerated source.</summary>
    object?[] StratifiedValues(IEnumerable values, double[] weights, int count);

    /// <summary>Returns systematic-resampling ancestor indexes.</summary>
    int[] SystematicIndexes(double[] weights, int count);

    /// <summary>Fills a destination with systematic-resampling ancestor indexes.</summary>
    void FillSystematicIndexes(double[] weights, Span<int> destination);

    /// <summary>Returns systematic-resampled values from an enumerated source.</summary>
    object?[] SystematicValues(IEnumerable values, double[] weights, int count);

    /// <summary>Returns residual-resampling ancestor indexes.</summary>
    int[] ResidualIndexes(double[] weights, int count);

    /// <summary>Fills a destination with residual-resampling ancestor indexes.</summary>
    void FillResidualIndexes(double[] weights, Span<int> destination);

    /// <summary>Returns residual-resampled values from an enumerated source.</summary>
    object?[] ResidualValues(IEnumerable values, double[] weights, int count);
}
