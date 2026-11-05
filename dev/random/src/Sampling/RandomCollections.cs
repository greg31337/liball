using System;

namespace random;

/// <summary>
/// Provides deterministic random operations over caller-owned collections.
/// </summary>
/// <remarks>
/// Results depend on the source collection's iteration order as well as the generator and call
/// sequence. Integer-only operations replay bit-for-bit for a named deterministic engine. Weighted
/// operations that invoke mathematical kernels follow the generator's Portable or System replay level.
/// No method is suitable for choosing secrets; use <see cref="SecureRandomGenerator"/> for security-sensitive output.
/// </remarks>
internal static class RandomCollections
{
    /// <summary>Returns uniformly sampled bootstrap indexes with replacement.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="populationSize">The positive exclusive upper index bound.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count indexes in the population bounds.</returns>
    /// <exception cref="RandomArgumentException">The generator is null, the population size is not positive, or the count is negative.</exception>
    public static int[] BootstrapIndexes(IRandomSource generator, int populationSize, int count)
    {
        return Resampling.BootstrapIndexes(generator, populationSize, count);
    }

    /// <summary>Fills a destination with uniformly sampled bootstrap indexes.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="populationSize">The positive exclusive upper index bound.</param>
    /// <param name="destination">The destination indexes.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the population size is not positive.</exception>
    public static void FillBootstrapIndexes(IRandomSource generator, int populationSize, Span<int> destination)
    {
        Resampling.FillBootstrapIndexes(generator, populationSize, destination);
    }

    /// <summary>Returns stratified ancestor indexes from relative weights.</summary>
    /// <param name="generator">The generator that supplies one offset per output stratum.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count ancestor indexes.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the count is negative, or the weights are empty or invalid.</exception>
    public static int[] StratifiedResample(IRandomSource generator, double[] weights, int count)
    {
        return Resampling.StratifiedIndexes(generator, weights, count);
    }

    /// <summary>Fills a destination with stratified ancestor indexes from relative weights.</summary>
    /// <param name="generator">The generator that supplies one offset per output stratum.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="destination">The destination ancestor indexes.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the weights are empty or invalid.</exception>
    public static void FillStratifiedResample(IRandomSource generator, double[] weights, Span<int> destination)
    {
        Resampling.FillStratifiedIndexes(generator, weights, destination);
    }

    /// <summary>Returns systematic ancestor indexes from relative weights.</summary>
    /// <param name="generator">The generator that supplies the shared systematic offset.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count ancestor indexes.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the count is negative, or the weights are empty or invalid.</exception>
    public static int[] SystematicResample(IRandomSource generator, double[] weights, int count)
    {
        return Resampling.SystematicIndexes(generator, weights, count);
    }

    /// <summary>Fills a destination with systematic ancestor indexes from relative weights.</summary>
    /// <param name="generator">The generator that supplies the shared systematic offset.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="destination">The destination ancestor indexes.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the weights are empty or invalid.</exception>
    public static void FillSystematicResample(IRandomSource generator, double[] weights, Span<int> destination)
    {
        Resampling.FillSystematicIndexes(generator, weights, destination);
    }

    /// <summary>Returns residual ancestor indexes from relative weights.</summary>
    /// <param name="generator">The generator that supplies fractional-residual draws.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count ancestor indexes.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the count is negative, or the weights are empty or invalid.</exception>
    public static int[] ResidualResample(IRandomSource generator, double[] weights, int count)
    {
        return Resampling.ResidualIndexes(generator, weights, count);
    }

    /// <summary>Fills a destination with residual ancestor indexes from relative weights.</summary>
    /// <param name="generator">The generator that supplies fractional-residual draws.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="destination">The destination ancestor indexes.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the weights are empty or invalid.</exception>
    public static void FillResidualResample(IRandomSource generator, double[] weights, Span<int> destination)
    {
        Resampling.FillResidualIndexes(generator, weights, destination);
    }

    /// <summary>Returns an ordered weighted sample of distinct category indexes.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="weights">The nonnegative relative weights, which may include positive infinity.</param>
    /// <param name="count">The number of distinct indexes to sample.</param>
    /// <returns>Distinct indexes in exponential-race order.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, a weight is negative or NaN, the count is negative, or fewer than count weights are positive.</exception>
    public static int[] WeightedSampleWithoutReplacement(IRandomSource generator, double[] weights, int count)
    {
        return WeightedWithoutReplacement.SampleIndexes(generator, weights, count);
    }

    /// <summary>Fills a destination with an ordered weighted sample of distinct category indexes.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="weights">The nonnegative relative weights, which may include positive infinity.</param>
    /// <param name="destination">The destination whose length determines the sample count.</param>
    /// <exception cref="RandomArgumentException">An argument is null, a weight is negative or NaN, or fewer than the destination length weights are positive.</exception>
    public static void FillWeightedSampleWithoutReplacement(IRandomSource generator, double[] weights, Span<int> destination)
    {
        WeightedWithoutReplacement.FillIndexes(generator, weights, destination);
    }

    /// <summary>Returns a uniformly shuffled permutation of the integers from zero to a count exclusive.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="count">The nonnegative permutation length.</param>
    /// <returns>A permutation containing every integer from zero through count minus one.</returns>
    /// <exception cref="RandomArgumentException">The generator is null or the count is negative.</exception>
    public static int[] Permutation(IRandomSource generator, int count)
    {
        return PermutationSampler.Permutation(generator, count);
    }

    /// <summary>Returns a uniformly selected permutation with no fixed points.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="count">The nonnegative derangement length.</param>
    /// <returns>A permutation in which every value differs from its position.</returns>
    /// <remarks>
    /// Count zero returns the unique empty derangement. Count one is impossible and is rejected.
    /// Count two returns its unique derangement without consuming random state. Larger counts use
    /// exact rejection from uniform permutations.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the count is negative, or the count is one.</exception>
    public static int[] Derangement(IRandomSource generator, int count)
    {
        return PermutationSampler.Derangement(generator, count);
    }

    /// <summary>Returns a uniformly selected subset of integer indexes in ascending order.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="populationSize">The nonnegative exclusive upper bound for selected indexes.</param>
    /// <param name="count">The number of indexes to select.</param>
    /// <returns>Distinct selected indexes in ascending order.</returns>
    /// <remarks>Sizes zero and populationSize are deterministic and consume no random state.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the population size is negative, or the count is outside the population bounds.</exception>
    public static int[] Subset(IRandomSource generator, int populationSize, int count)
    {
        return PermutationSampler.Subset(generator, populationSize, count);
    }

    /// <summary>Returns a uniformly selected integer combination in ascending order.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="populationSize">The nonnegative exclusive upper bound for selected indexes.</param>
    /// <param name="count">The number of indexes in the combination.</param>
    /// <returns>Distinct selected indexes in ascending order.</returns>
    /// <remarks>This is a naming alias for <see cref="Subset(IRandomSource, int, int)"/>.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the population size is negative, or the count is outside the population bounds.</exception>
    public static int[] Combination(IRandomSource generator, int populationSize, int count)
    {
        return PermutationSampler.Subset(generator, populationSize, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a one-dimensional array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>A zero-based array whose runtime element type matches the source.</returns>
    /// <remarks>The source is not mutated.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty or not one-dimensional, or the count is negative.</exception>
    public static Array BootstrapSample(IRandomSource generator, Array values, int count)
    {
        return BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns an object-array bootstrap sample with replacement from a list.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source list.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count values selected with replacement.</returns>
    /// <remarks>The source list is not mutated.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static object?[] BootstrapSample(IRandomSource generator, System.Collections.IList values, int count)
    {
        ValidateListSource(generator, values);
        Guard.Count(count, "count");
        int[] indexes = Resampling.BootstrapIndexes(generator, values.Count, count);
        object?[] result = new object?[count];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = values[indexes[index]];
            index++;
        }

        return result;
    }

    /// <summary>Returns an ordered sample without replacement from a one-dimensional array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>A zero-based array whose runtime element type matches the source.</returns>
    /// <remarks>The source is not mutated. Equal source values may both appear when they occupy different source positions.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null, the source is not one-dimensional, or the count is outside the source bounds.</exception>
    public static Array SampleWithoutReplacement(IRandomSource generator, Array values, int count)
    {
        ValidateSampleArray(generator, values, count);
        return SampleWithoutReplacementArrayCore(generator, values, count);
    }

    /// <summary>Returns an ordered object-array sample without replacement from a list.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source list.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>An ordered sample of distinct source positions.</returns>
    /// <remarks>The source list is not mutated.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static object?[] SampleWithoutReplacement(IRandomSource generator, System.Collections.IList values, int count)
    {
        ValidateSampleList(generator, values, count);
        if (count == 0)
        {
            return new object?[0];
        }

        if (PermutationSampler.UseSparsePath(values.Count, count))
        {
            int[] indexes = PermutationSampler.SparseOrderedIndexes(generator, values.Count, count);
            object?[] sparseResult = new object?[count];
            int sparseIndex = 0;
            while (sparseIndex < sparseResult.Length)
            {
                sparseResult[sparseIndex] = values[indexes[sparseIndex]];
                sparseIndex++;
            }

            return sparseResult;
        }

        object?[] candidates = CopyList(values);
        PartialShuffleArrayCore(generator, candidates, count);
        return CopyArrayPrefix(candidates, count);
    }

    /// <summary>Returns a one-pass reservoir sample from a streaming source.</summary>
    /// <param name="generator">The generator that supplies replacement indexes.</param>
    /// <param name="values">The source sequence.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>An Algorithm R reservoir containing elements selected without replacement by source position.</returns>
    /// <remarks>The source is enumerated exactly once in its natural order. A zero count returns without requesting an enumerator.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null, the count is negative, or the completed source contains fewer elements than requested.</exception>
    /// <exception cref="RandomStateException">The source traversal exceeds the supported signed 64-bit element count.</exception>
    public static object?[] ReservoirSample(IRandomSource generator, System.Collections.IEnumerable values, int count)
    {
        return ReservoirSampler.Sample(generator, values, count);
    }

    /// <summary>Returns a reservoir sample from a random-access list.</summary>
    /// <param name="generator">The generator that supplies replacement indexes.</param>
    /// <param name="values">The source list.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>An Algorithm R reservoir containing elements selected without replacement by source position.</returns>
    /// <remarks>For a positive count, list positions are read exactly once from index zero upward. A zero count reads no elements.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static object?[] ReservoirSample(IRandomSource generator, System.Collections.IList values, int count)
    {
        return ReservoirSampler.Sample(generator, values, count);
    }

    /// <summary>Chooses one uniformly distributed element from a one-dimensional array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty one-dimensional source array.</param>
    /// <returns>The selected element.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty or not one-dimensional.</exception>
    public static object? Choose(IRandomSource generator, Array values)
    {
        ValidateArraySource(generator, values);
        return ChooseArrayCore(generator, values);
    }

    /// <summary>Chooses one uniformly distributed element from a list.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source list.</param>
    /// <returns>The selected element.</returns>
    /// <exception cref="RandomArgumentException">The generator or list is null, or the list is empty.</exception>
    public static object? Choose(IRandomSource generator, System.Collections.IList values)
    {
        ValidateListSource(generator, values);
        return values[IntegerUniform.NextInt32(generator, values.Count)];
    }

    /// <summary>Fills an array with independent uniform choices from a one-dimensional array.</summary>
    /// <param name="generator">The generator that supplies the random indexes.</param>
    /// <param name="values">The nonempty one-dimensional source array.</param>
    /// <param name="destination">The one-dimensional destination array.</param>
    /// <remarks>The source and destination may be the same array; choices always use the original source values.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null, an array is not one-dimensional, the source is empty, or a source element cannot be stored in the destination.</exception>
    public static void FillChoices(IRandomSource generator, Array values, Array destination)
    {
        ValidateArraySource(generator, values);
        ValidateArrayDestination(values, destination);
        FillChoicesArrayCore(generator, values, destination);
    }

    /// <summary>Fills an array with independent uniform choices from a list.</summary>
    /// <param name="generator">The generator that supplies the random indexes.</param>
    /// <param name="values">The nonempty source list.</param>
    /// <param name="destination">The one-dimensional destination array.</param>
    /// <remarks>The choices are captured before destination writes, including when the list exposes the destination storage.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, the destination is not one-dimensional, or a source element cannot be stored in the destination.</exception>
    public static void FillChoices(IRandomSource generator, System.Collections.IList values, Array destination)
    {
        ValidateListSource(generator, values);
        ValidateArrayDestination(values, destination);
        FillChoicesListCore(generator, values, destination);
    }

    /// <summary>Returns a shuffled copy of a one-dimensional array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The one-dimensional source array.</param>
    /// <returns>A shuffled array with the same runtime type and bounds as the source.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is not one-dimensional.</exception>
    public static Array ShuffledCopy(IRandomSource generator, Array values)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        Guard.Argument(values.Rank == 1, "values", "The array must be one-dimensional.");
        Array result = (Array)values.Clone();
        ShuffleArrayCore(generator, result);
        return result;
    }

    /// <summary>Returns a shuffled object-array copy of a list.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source list.</param>
    /// <returns>A shuffled copy containing the list elements.</returns>
    /// <exception cref="RandomArgumentException">The generator or list is null.</exception>
    public static object?[] ShuffledCopy(IRandomSource generator, System.Collections.IList values)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        object?[] result = CopyList(values);
        ShuffleArrayCore(generator, result);
        return result;
    }

    /// <summary>Moves a uniform ordered sample into the requested number of prefix positions of an array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The one-dimensional array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null, the array is not one-dimensional, or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, Array values, int count)
    {
        ValidatePartialArray(generator, values, count);
        PartialShuffleArrayCore(generator, values, count);
    }

    /// <summary>Moves a uniform ordered sample into the requested number of prefix positions of a mutable list.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The mutable list to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null, the list is read-only, or the count is outside the list bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, System.Collections.IList values, int count)
    {
        ValidatePartialList(generator, values, count);
        PartialShuffleListCore(generator, values, count);
    }

    /// <summary>Moves a uniform ordered sample into the requested number of prefix positions of an array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The one-dimensional array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null, the array is not one-dimensional, or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, Array values, int count)
    {
        PartialShuffle(generator, values, count);
    }

    /// <summary>Moves a uniform ordered sample into the requested number of prefix positions of a mutable list.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The mutable list to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null, the list is read-only, or the count is outside the list bounds.</exception>
    public static void Shuffle(IRandomSource generator, System.Collections.IList values, int count)
    {
        PartialShuffle(generator, values, count);
    }

    /// <summary>Chooses one Boolean value uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static bool Choose(IRandomSource generator, bool[] values)
    {
        return (bool)ChooseValueType(generator, values);
    }

    /// <summary>Chooses one unsigned byte uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static byte Choose(IRandomSource generator, byte[] values)
    {
        return (byte)ChooseValueType(generator, values);
    }

    /// <summary>Chooses one signed byte uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static sbyte Choose(IRandomSource generator, sbyte[] values)
    {
        return (sbyte)ChooseValueType(generator, values);
    }

    /// <summary>Chooses one signed 16-bit integer uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static short Choose(IRandomSource generator, short[] values)
    {
        return (short)ChooseValueType(generator, values);
    }

    /// <summary>Chooses one unsigned 16-bit integer uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static ushort Choose(IRandomSource generator, ushort[] values)
    {
        return (ushort)ChooseValueType(generator, values);
    }

    /// <summary>Chooses one signed 32-bit integer uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static int Choose(IRandomSource generator, int[] values)
    {
        return (int)ChooseValueType(generator, values);
    }

    /// <summary>Chooses one unsigned 32-bit integer uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static uint Choose(IRandomSource generator, uint[] values)
    {
        return (uint)ChooseValueType(generator, values);
    }

    /// <summary>Chooses one signed 64-bit integer uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static long Choose(IRandomSource generator, long[] values)
    {
        return (long)ChooseValueType(generator, values);
    }

    /// <summary>Chooses one unsigned 64-bit integer uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static ulong Choose(IRandomSource generator, ulong[] values)
    {
        return (ulong)ChooseValueType(generator, values);
    }

    /// <summary>Chooses one character uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static char Choose(IRandomSource generator, char[] values)
    {
        return (char)ChooseValueType(generator, values);
    }

    /// <summary>Chooses one single-precision value uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static float Choose(IRandomSource generator, float[] values)
    {
        return (float)ChooseValueType(generator, values);
    }

    /// <summary>Chooses one double-precision value uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static double Choose(IRandomSource generator, double[] values)
    {
        return (double)ChooseValueType(generator, values);
    }

    /// <summary>Chooses one decimal value uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static decimal Choose(IRandomSource generator, decimal[] values)
    {
        return (decimal)ChooseValueType(generator, values);
    }

    /// <summary>Chooses one string uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static string? Choose(IRandomSource generator, string?[] values)
    {
        return (string?)ChooseTyped(generator, values);
    }

    /// <summary>Chooses one object uniformly from an array.</summary>
    /// <param name="generator">The generator that supplies the random index.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <returns>The selected value.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is empty.</exception>
    public static object? Choose(IRandomSource generator, object?[] values)
    {
        return ChooseTyped(generator, values);
    }

    /// <summary>Fills a Boolean array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, bool[] values, bool[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills an unsigned byte array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, byte[] values, byte[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills a signed byte array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, sbyte[] values, sbyte[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills a signed 16-bit integer array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, short[] values, short[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills an unsigned 16-bit integer array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, ushort[] values, ushort[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills a signed 32-bit integer array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, int[] values, int[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills an unsigned 32-bit integer array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, uint[] values, uint[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills a signed 64-bit integer array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, long[] values, long[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills an unsigned 64-bit integer array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, ulong[] values, ulong[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills a character array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, char[] values, char[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills a single-precision array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, float[] values, float[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills a double-precision array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, double[] values, double[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills a decimal array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, decimal[] values, decimal[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills a string array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, string?[] values, string?[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Fills an object array with independent uniform choices.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="destination">The destination array.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the source is empty.</exception>
    public static void FillChoices(IRandomSource generator, object?[] values, object?[] destination)
    {
        FillChoicesTyped(generator, values, destination);
    }

    /// <summary>Returns a bootstrap sample with replacement from a Boolean array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static bool[] BootstrapSample(IRandomSource generator, bool[] values, int count)
    {
        return (bool[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a unsigned-byte array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static byte[] BootstrapSample(IRandomSource generator, byte[] values, int count)
    {
        return (byte[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a signed-byte array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static sbyte[] BootstrapSample(IRandomSource generator, sbyte[] values, int count)
    {
        return (sbyte[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a signed 16-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static short[] BootstrapSample(IRandomSource generator, short[] values, int count)
    {
        return (short[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a unsigned 16-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static ushort[] BootstrapSample(IRandomSource generator, ushort[] values, int count)
    {
        return (ushort[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a signed 32-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static int[] BootstrapSample(IRandomSource generator, int[] values, int count)
    {
        return (int[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a unsigned 32-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static uint[] BootstrapSample(IRandomSource generator, uint[] values, int count)
    {
        return (uint[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a signed 64-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static long[] BootstrapSample(IRandomSource generator, long[] values, int count)
    {
        return (long[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a unsigned 64-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static ulong[] BootstrapSample(IRandomSource generator, ulong[] values, int count)
    {
        return (ulong[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a character array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static char[] BootstrapSample(IRandomSource generator, char[] values, int count)
    {
        return (char[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a single-precision array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static float[] BootstrapSample(IRandomSource generator, float[] values, int count)
    {
        return (float[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a double-precision array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static double[] BootstrapSample(IRandomSource generator, double[] values, int count)
    {
        return (double[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a decimal array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static decimal[] BootstrapSample(IRandomSource generator, decimal[] values, int count)
    {
        return (decimal[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a string array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static string?[] BootstrapSample(IRandomSource generator, string?[] values, int count)
    {
        return (string?[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns a bootstrap sample with replacement from a object array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The nonempty source array.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the source is empty, or the count is negative.</exception>
    public static object?[] BootstrapSample(IRandomSource generator, object?[] values, int count)
    {
        return (object?[])BootstrapSampleTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from a Boolean array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static bool[] SampleWithoutReplacement(IRandomSource generator, bool[] values, int count)
    {
        return (bool[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from an unsigned byte array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static byte[] SampleWithoutReplacement(IRandomSource generator, byte[] values, int count)
    {
        return (byte[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from a signed byte array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static sbyte[] SampleWithoutReplacement(IRandomSource generator, sbyte[] values, int count)
    {
        return (sbyte[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from a signed 16-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static short[] SampleWithoutReplacement(IRandomSource generator, short[] values, int count)
    {
        return (short[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from an unsigned 16-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static ushort[] SampleWithoutReplacement(IRandomSource generator, ushort[] values, int count)
    {
        return (ushort[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from a signed 32-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static int[] SampleWithoutReplacement(IRandomSource generator, int[] values, int count)
    {
        return (int[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from an unsigned 32-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static uint[] SampleWithoutReplacement(IRandomSource generator, uint[] values, int count)
    {
        return (uint[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from a signed 64-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static long[] SampleWithoutReplacement(IRandomSource generator, long[] values, int count)
    {
        return (long[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from an unsigned 64-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static ulong[] SampleWithoutReplacement(IRandomSource generator, ulong[] values, int count)
    {
        return (ulong[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from a character array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static char[] SampleWithoutReplacement(IRandomSource generator, char[] values, int count)
    {
        return (char[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from a single-precision array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static float[] SampleWithoutReplacement(IRandomSource generator, float[] values, int count)
    {
        return (float[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from a double-precision array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static double[] SampleWithoutReplacement(IRandomSource generator, double[] values, int count)
    {
        return (double[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from a decimal array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static decimal[] SampleWithoutReplacement(IRandomSource generator, decimal[] values, int count)
    {
        return (decimal[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from a string array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static string?[] SampleWithoutReplacement(IRandomSource generator, string?[] values, int count)
    {
        return (string?[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns an ordered sample without replacement from an object array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <param name="count">The number of elements to sample.</param>
    /// <returns>The sampled values.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the source bounds.</exception>
    public static object?[] SampleWithoutReplacement(IRandomSource generator, object?[] values, int count)
    {
        return (object?[])SampleWithoutReplacementTyped(generator, values, count);
    }

    /// <summary>Returns a shuffled copy of a Boolean array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static bool[] ShuffledCopy(IRandomSource generator, bool[] values)
    {
        return (bool[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of an unsigned byte array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static byte[] ShuffledCopy(IRandomSource generator, byte[] values)
    {
        return (byte[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of a signed byte array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static sbyte[] ShuffledCopy(IRandomSource generator, sbyte[] values)
    {
        return (sbyte[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of a signed 16-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static short[] ShuffledCopy(IRandomSource generator, short[] values)
    {
        return (short[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of an unsigned 16-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static ushort[] ShuffledCopy(IRandomSource generator, ushort[] values)
    {
        return (ushort[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of a signed 32-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static int[] ShuffledCopy(IRandomSource generator, int[] values)
    {
        return (int[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of an unsigned 32-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static uint[] ShuffledCopy(IRandomSource generator, uint[] values)
    {
        return (uint[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of a signed 64-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static long[] ShuffledCopy(IRandomSource generator, long[] values)
    {
        return (long[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of an unsigned 64-bit integer array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static ulong[] ShuffledCopy(IRandomSource generator, ulong[] values)
    {
        return (ulong[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of a character array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static char[] ShuffledCopy(IRandomSource generator, char[] values)
    {
        return (char[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of a single-precision array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static float[] ShuffledCopy(IRandomSource generator, float[] values)
    {
        return (float[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of a double-precision array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static double[] ShuffledCopy(IRandomSource generator, double[] values)
    {
        return (double[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of a decimal array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static decimal[] ShuffledCopy(IRandomSource generator, decimal[] values)
    {
        return (decimal[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of a string array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static string?[] ShuffledCopy(IRandomSource generator, string?[] values)
    {
        return (string?[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Returns a shuffled copy of an object array.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The source array.</param>
    /// <returns>The shuffled copy.</returns>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static object?[] ShuffledCopy(IRandomSource generator, object?[] values)
    {
        return (object?[])ShuffledTypedCopy(generator, values);
    }

    /// <summary>Partially shuffles a Boolean array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, bool[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles an unsigned byte array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, byte[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a signed byte array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, sbyte[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a signed 16-bit integer array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, short[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles an unsigned 16-bit integer array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, ushort[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a signed 32-bit integer array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, int[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles an unsigned 32-bit integer array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, uint[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a signed 64-bit integer array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, long[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles an unsigned 64-bit integer array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, ulong[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a character array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, char[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a single-precision array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, float[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a double-precision array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, double[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a decimal array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, decimal[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a string array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, string?[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles an object array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void PartialShuffle(IRandomSource generator, object?[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a Boolean array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, bool[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles an unsigned byte array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, byte[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a signed byte array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, sbyte[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a signed 16-bit integer array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, short[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles an unsigned 16-bit integer array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, ushort[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a signed 32-bit integer array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, int[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles an unsigned 32-bit integer array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, uint[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a signed 64-bit integer array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, long[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles an unsigned 64-bit integer array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, ulong[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a character array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, char[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a single-precision array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, float[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a double-precision array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, double[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a decimal array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, decimal[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles a string array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, string?[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>Partially shuffles an object array into a random prefix.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to partially shuffle.</param>
    /// <param name="count">The number of prefix positions to randomize.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the count is outside the array bounds.</exception>
    public static void Shuffle(IRandomSource generator, object?[] values, int count)
    {
        PartialShuffleTyped(generator, values, count);
    }

    /// <summary>
    /// Shuffles a one-dimensional array in place using the Fisher-Yates algorithm.
    /// </summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The one-dimensional array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null, or the array is not one-dimensional.</exception>
    public static void Shuffle(IRandomSource generator, Array values)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        Guard.Argument(values.Rank == 1, "values", "The array must be one-dimensional.");
        ShuffleArrayCore(generator, values);
    }

    /// <summary>
    /// Shuffles a mutable list in place using the Fisher-Yates algorithm.
    /// </summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The mutable list to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or list is null, or the list is read-only.</exception>
    public static void Shuffle(IRandomSource generator, System.Collections.IList values)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        Guard.Argument(!values.IsReadOnly, "values", "The list must permit replacing elements.");
        ShuffleListCore(generator, values);
    }

    /// <summary>Shuffles a Boolean array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, bool[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles an unsigned byte array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, byte[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles a signed byte array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, sbyte[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles a signed 16-bit integer array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, short[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles an unsigned 16-bit integer array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, ushort[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles a signed 32-bit integer array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, int[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles an unsigned 32-bit integer array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, uint[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles a signed 64-bit integer array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, long[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles an unsigned 64-bit integer array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, ulong[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles a character array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, char[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles a single-precision floating-point array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, float[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles a double-precision floating-point array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, double[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles a decimal array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, decimal[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles a string array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, string?[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    /// <summary>Shuffles an object array in place using the Fisher-Yates algorithm.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="values">The array to shuffle.</param>
    /// <exception cref="RandomArgumentException">The generator or array is null.</exception>
    public static void Shuffle(IRandomSource generator, object?[] values)
    {
        ShuffleTypedArray(generator, values);
    }

    static Array BootstrapSampleTyped(IRandomSource generator, Array values, int count)
    {
        ValidateArraySource(generator, values);
        Guard.Count(count, "count");
        int[] indexes = Resampling.BootstrapIndexes(generator, values.Length, count);
        System.Type elementType = ArrayElementType(values);
        Array result = Array.CreateInstance(elementType, count);
        int lowerBound = values.GetLowerBound(0);
        int index = 0;
        while (index < count)
        {
            result.SetValue(values.GetValue(lowerBound + indexes[index]), index);
            index++;
        }

        return result;
    }

    static void ValidateSampleArray(IRandomSource generator, Array values, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        Guard.Argument(values.Rank == 1, "values", "The source array must be one-dimensional.");
        Guard.Argument(count >= 0 && count <= values.Length, "count", "The count must be within the source bounds.");
    }

    static void ValidateSampleList(IRandomSource generator, System.Collections.IList values, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        Guard.Argument(count >= 0 && count <= values.Count, "count", "The count must be within the source bounds.");
    }

    static Array SampleWithoutReplacementTyped(IRandomSource generator, Array values, int count)
    {
        ValidateSampleArray(generator, values, count);
        return SampleWithoutReplacementArrayCore(generator, values, count);
    }

    static Array SampleWithoutReplacementArrayCore(IRandomSource generator, Array values, int count)
    {
        System.Type elementType = ArrayElementType(values);
        if (count == 0)
        {
            return Array.CreateInstance(elementType, 0);
        }

        if (PermutationSampler.UseSparsePath(values.Length, count))
        {
            int[] indexes = PermutationSampler.SparseOrderedIndexes(generator, values.Length, count);
            Array sparseResult = Array.CreateInstance(elementType, count);
            int valuesLowerBound = values.GetLowerBound(0);
            int sparseIndex = 0;
            while (sparseIndex < count)
            {
                sparseResult.SetValue(values.GetValue(valuesLowerBound + indexes[sparseIndex]), sparseIndex);
                sparseIndex++;
            }

            return sparseResult;
        }

        Array candidates = (Array)values.Clone();
        PartialShuffleArrayCore(generator, candidates, count);
        Array result = Array.CreateInstance(elementType, count);
        int sourceLowerBound = candidates.GetLowerBound(0);
        int index = 0;
        while (index < count)
        {
            result.SetValue(candidates.GetValue(sourceLowerBound + index), index);
            index++;
        }

        return result;
    }

    static object?[] CopyArrayPrefix(object?[] values, int count)
    {
        object?[] result = new object?[count];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = values[index];
            index++;
        }

        return result;
    }

    static void ValidateArraySource(IRandomSource generator, Array values)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        Guard.Argument(values.Rank == 1, "values", "The source array must be one-dimensional.");
        Guard.Argument(values.Length > 0, "values", "The source array must not be empty.");
    }

    static void ValidateListSource(IRandomSource generator, System.Collections.IList values)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        Guard.Argument(values.Count > 0, "values", "The source list must not be empty.");
    }

    static void ValidateArrayDestination(Array values, Array destination)
    {
        Guard.NotNull(destination, "destination");
        Guard.Argument(destination.Rank == 1, "destination", "The destination array must be one-dimensional.");
        System.Type elementType = ArrayElementType(destination);
        int lowerBound = values.GetLowerBound(0);
        int index = 0;
        while (index < values.Length)
        {
            ValidateDestinationElement(values.GetValue(lowerBound + index), elementType);
            index++;
        }
    }

    static void ValidateArrayDestination(System.Collections.IList values, Array destination)
    {
        Guard.NotNull(destination, "destination");
        Guard.Argument(destination.Rank == 1, "destination", "The destination array must be one-dimensional.");
        System.Type elementType = ArrayElementType(destination);
        int index = 0;
        while (index < values.Count)
        {
            ValidateDestinationElement(values[index], elementType);
            index++;
        }
    }

    static void ValidateDestinationElement(object? value, System.Type elementType)
    {
        if (value == null)
        {
            Guard.Argument(!elementType.IsValueType, "destination", "The destination cannot store every source element.");
            return;
        }

        Guard.Argument(elementType.IsInstanceOfType(value), "destination", "The destination cannot store every source element.");
    }

    static System.Type ArrayElementType(Array values)
    {
        System.Type? elementType = values.GetType().GetElementType();
        if (elementType == null)
        {
            throw RandomStateException.Create("The array runtime type has no element type.");
        }

        return elementType;
    }

    static object? ChooseTyped(IRandomSource generator, Array values)
    {
        ValidateArraySource(generator, values);
        return ChooseArrayCore(generator, values);
    }

    static object ChooseValueType(IRandomSource generator, Array values)
    {
        object? selected = ChooseTyped(generator, values);
        if (selected == null)
        {
            throw RandomStateException.Create("A value-type array returned a null element.");
        }

        return selected;
    }

    static object? ChooseArrayCore(IRandomSource generator, Array values)
    {
        int selected = IntegerUniform.NextInt32(generator, values.Length);
        return values.GetValue(values.GetLowerBound(0) + selected);
    }

    static void FillChoicesTyped(IRandomSource generator, Array values, Array destination)
    {
        ValidateArraySource(generator, values);
        ValidateArrayDestination(values, destination);
        FillChoicesArrayCore(generator, values, destination);
    }

    static void FillChoicesArrayCore(IRandomSource generator, Array values, Array destination)
    {
        object?[] selections = new object?[destination.Length];
        int sourceLowerBound = values.GetLowerBound(0);
        int index = 0;
        while (index < selections.Length)
        {
            int selected = IntegerUniform.NextInt32(generator, values.Length);
            selections[index] = values.GetValue(sourceLowerBound + selected);
            index++;
        }

        FillSelectionArray(selections, destination);
    }

    static void FillChoicesListCore(IRandomSource generator, System.Collections.IList values, Array destination)
    {
        object?[] selections = new object?[destination.Length];
        int index = 0;
        while (index < selections.Length)
        {
            selections[index] = values[IntegerUniform.NextInt32(generator, values.Count)];
            index++;
        }

        FillSelectionArray(selections, destination);
    }

    static void FillSelectionArray(object?[] selections, Array destination)
    {
        int destinationLowerBound = destination.GetLowerBound(0);
        int index = 0;
        while (index < selections.Length)
        {
            destination.SetValue(selections[index], destinationLowerBound + index);
            index++;
        }
    }

    static Array ShuffledTypedCopy(IRandomSource generator, Array values)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        Array result = (Array)values.Clone();
        ShuffleArrayCore(generator, result);
        return result;
    }

    static object?[] CopyList(System.Collections.IList values)
    {
        object?[] result = new object?[values.Count];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = values[index];
            index++;
        }

        return result;
    }

    static void ValidatePartialArray(IRandomSource generator, Array values, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        Guard.Argument(values.Rank == 1, "values", "The array must be one-dimensional.");
        Guard.Argument(count >= 0 && count <= values.Length, "count", "The count must be within the array bounds.");
    }

    static void ValidatePartialList(IRandomSource generator, System.Collections.IList values, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        Guard.Argument(!values.IsReadOnly, "values", "The list must permit replacing elements.");
        Guard.Argument(count >= 0 && count <= values.Count, "count", "The count must be within the list bounds.");
    }

    static void PartialShuffleTyped(IRandomSource generator, Array values, int count)
    {
        ValidatePartialArray(generator, values, count);
        PartialShuffleArrayCore(generator, values, count);
    }

    static void PartialShuffleArrayCore(IRandomSource generator, Array values, int count)
    {
        int lowerBound = values.GetLowerBound(0);
        int current = 0;
        while (current < count)
        {
            int selected = current + IntegerUniform.NextInt32(generator, values.Length - current);
            if (selected != current)
            {
                int currentIndex = lowerBound + current;
                int selectedIndex = lowerBound + selected;
                object? temporary = values.GetValue(currentIndex);
                values.SetValue(values.GetValue(selectedIndex), currentIndex);
                values.SetValue(temporary, selectedIndex);
            }

            current++;
        }
    }

    static void PartialShuffleListCore(IRandomSource generator, System.Collections.IList values, int count)
    {
        int current = 0;
        while (current < count)
        {
            int selected = current + IntegerUniform.NextInt32(generator, values.Count - current);
            if (selected != current)
            {
                object? temporary = values[current];
                values[current] = values[selected];
                values[selected] = temporary;
            }

            current++;
        }
    }

    static void ShuffleTypedArray(IRandomSource generator, Array values)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(values, "values");
        ShuffleArrayCore(generator, values);
    }

    static void ShuffleArrayCore(IRandomSource generator, Array values)
    {
        int lowerBound = values.GetLowerBound(0);
        int current = values.Length - 1;
        while (current > 0)
        {
            int selected = IntegerUniform.NextInt32(generator, current + 1);
            if (selected != current)
            {
                int currentIndex = lowerBound + current;
                int selectedIndex = lowerBound + selected;
                object? temporary = values.GetValue(currentIndex);
                values.SetValue(values.GetValue(selectedIndex), currentIndex);
                values.SetValue(temporary, selectedIndex);
            }

            current--;
        }
    }

    static void ShuffleListCore(IRandomSource generator, System.Collections.IList values)
    {
        int current = values.Count - 1;
        while (current > 0)
        {
            int selected = IntegerUniform.NextInt32(generator, current + 1);
            if (selected != current)
            {
                object? temporary = values[current];
                values[current] = values[selected];
                values[selected] = temporary;
            }

            current--;
        }
    }
}
