using System;

namespace random;

/// <summary>
/// Provides weighted index sampling without replacement through exponential-race keys.
/// </summary>
internal static class WeightedWithoutReplacement
{
    /// <summary>Returns an ordered weighted sample of distinct category indexes.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="weights">The nonnegative relative weights, which may include positive infinity.</param>
    /// <param name="count">The number of distinct indexes to sample.</param>
    /// <returns>Distinct indexes in exponential-race order.</returns>
    /// <remarks>
    /// Zero weights are ineligible. Positive-infinite weights precede every finite weight and
    /// are uniformly ordered among themselves. Finite positive weights use logarithms of
    /// exponential keys so extreme finite weights do not overflow or underflow during ranking.
    /// </remarks>
    /// <exception cref="RandomArgumentException">An argument is null, a weight is negative or NaN, the count is negative, or fewer than count weights are positive.</exception>
    public static int[] SampleIndexes(IRandomSource generator, double[] weights, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(weights, "weights");
        Guard.Count(count, "count");

        int infiniteCount = 0;
        int finiteCount = 0;
        int index = 0;
        while (index < weights.Length)
        {
            double weight = weights[index];
            if (double.IsNaN(weight) || weight < 0.0 || double.IsNegativeInfinity(weight))
            {
                throw RandomArgumentException.Create("weights", "Every weight must be nonnegative and must not be NaN or negative infinity.");
            }

            if (double.IsPositiveInfinity(weight))
            {
                infiniteCount++;
            }
            else if (weight > 0.0)
            {
                finiteCount++;
            }

            index++;
        }

        Guard.Argument(count <= infiniteCount + finiteCount, "count", "The count must not exceed the number of positive weights.");
        int[] result = new int[count];
        if (count == 0)
        {
            return result;
        }

        int[] infiniteIndexes = CollectIndexes(weights, true, infiniteCount);
        int infiniteTake = count;
        if (infiniteTake > infiniteCount)
        {
            infiniteTake = infiniteCount;
        }

        if (infiniteTake > 0)
        {
            RandomCollections.PartialShuffle(generator, infiniteIndexes, infiniteTake);
            Array.Copy(infiniteIndexes, result, infiniteTake);
        }

        if (infiniteTake == count)
        {
            return result;
        }

        int remaining = count - infiniteTake;
        int[] finiteIndexes = CollectIndexes(weights, false, finiteCount);
        if (finiteCount == 1)
        {
            result[infiniteTake] = finiteIndexes[0];
            return result;
        }

        double[] keys = new double[finiteCount];
        RandomMathMode mathMode = generator.MathMode();
        index = 0;
        while (index < finiteIndexes.Length)
        {
            double unit = FloatingUniform.NextDoubleOpenOpen(generator);
            double exponential = -Log(unit, mathMode);
            keys[index] = Log(exponential, mathMode) - Log(weights[finiteIndexes[index]], mathMode);
            index++;
        }

        Array.Sort(keys, finiteIndexes);
        Array.Copy(finiteIndexes, 0, result, infiniteTake, remaining);
        return result;
    }

    /// <summary>Returns an ordered weighted sample of distinct category indexes.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="weights">The nonnegative relative weights, which may include positive infinity.</param>
    /// <param name="count">The number of distinct indexes to sample.</param>
    /// <returns>Distinct indexes in exponential-race order.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, a weight is negative or NaN, the count is negative, or fewer than count weights are positive.</exception>
    public static int[] Sample(IRandomSource generator, double[] weights, int count)
    {
        return SampleIndexes(generator, weights, count);
    }

    /// <summary>Fills a destination with an ordered weighted sample of distinct category indexes.</summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="weights">The nonnegative relative weights, which may include positive infinity.</param>
    /// <param name="destination">The destination whose length determines the sample count.</param>
    /// <exception cref="RandomArgumentException">An argument is null, a weight is negative or NaN, or fewer than the destination length weights are positive.</exception>
    public static void FillIndexes(IRandomSource generator, double[] weights, Span<int> destination)
    {
        int[] result = SampleIndexes(generator, weights, destination.Length);
        result.CopyTo(destination);
    }

    static int[] CollectIndexes(double[] weights, bool infinite, int count)
    {
        int[] result = new int[count];
        int resultIndex = 0;
        int index = 0;
        while (index < weights.Length)
        {
            bool include;
            if (infinite)
            {
                include = double.IsPositiveInfinity(weights[index]);
            }
            else
            {
                include = weights[index] > 0.0 && double.IsFinite(weights[index]);
            }

            if (include)
            {
                result[resultIndex] = index;
                resultIndex++;
            }

            index++;
        }

        return result;
    }

    static double Log(double value, RandomMathMode mathMode)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Log(value);
        }

        return Math.Log(value);
    }
}
