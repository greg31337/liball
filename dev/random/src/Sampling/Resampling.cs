using System;

namespace random;

/// <summary>
/// Provides bootstrap and weighted ancestor-index resampling algorithms.
/// </summary>
internal static class Resampling
{
    /// <summary>Returns uniformly sampled bootstrap indexes with replacement.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="populationSize">The positive exclusive upper index bound.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count indexes in the population bounds.</returns>
    /// <exception cref="RandomArgumentException">The generator is null, the population size is not positive, or the count is negative.</exception>
    public static int[] BootstrapIndexes(IRandomSource generator, int populationSize, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.PositiveCount(populationSize, "populationSize");
        Guard.Count(count, "count");
        int[] result = new int[count];
        FillBootstrapIndexes(generator, populationSize, result);
        return result;
    }

    /// <summary>Fills a destination with uniformly sampled bootstrap indexes.</summary>
    /// <param name="generator">The generator that supplies random indexes.</param>
    /// <param name="populationSize">The positive exclusive upper index bound.</param>
    /// <param name="destination">The destination indexes.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the population size is not positive.</exception>
    public static void FillBootstrapIndexes(IRandomSource generator, int populationSize, Span<int> destination)
    {
        Guard.NotNull(generator, "generator");
        Guard.PositiveCount(populationSize, "populationSize");
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = IntegerUniform.NextInt32(generator, populationSize);
            index++;
        }
    }

    /// <summary>Returns stratified resampling indexes from relative weights.</summary>
    /// <param name="generator">The generator that supplies one offset per output stratum.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count ancestor indexes.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the count is negative, or the weights are empty or invalid.</exception>
    public static int[] StratifiedIndexes(IRandomSource generator, double[] weights, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(weights, "weights");
        Guard.Count(count, "count");
        int[] result = new int[count];
        FillStratifiedIndexes(generator, weights, result);
        return result;
    }

    /// <summary>Fills a destination with stratified resampling indexes.</summary>
    /// <param name="generator">The generator that supplies one offset per output stratum.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="destination">The destination ancestor indexes.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the weights are empty or invalid.</exception>
    public static void FillStratifiedIndexes(IRandomSource generator, double[] weights, Span<int> destination)
    {
        Guard.NotNull(generator, "generator");
        CategoricalProbabilityTable table = new CategoricalProbabilityTable(weights, "weights");
        if (FillDeterministic(table, destination))
        {
            return;
        }

        int index = 0;
        while (index < destination.Length)
        {
            double position = ((double)index + FloatingUniform.NextDoubleClosedOpen(generator)) / (double)destination.Length;
            destination[index] = FindCategory(table, position);
            index++;
        }
    }

    /// <summary>Returns systematic resampling indexes from relative weights.</summary>
    /// <param name="generator">The generator that supplies the shared systematic offset.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count ancestor indexes.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the count is negative, or the weights are empty or invalid.</exception>
    public static int[] SystematicIndexes(IRandomSource generator, double[] weights, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(weights, "weights");
        Guard.Count(count, "count");
        int[] result = new int[count];
        FillSystematicIndexes(generator, weights, result);
        return result;
    }

    /// <summary>Fills a destination with systematic resampling indexes.</summary>
    /// <param name="generator">The generator that supplies the shared systematic offset.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="destination">The destination ancestor indexes.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the weights are empty or invalid.</exception>
    public static void FillSystematicIndexes(IRandomSource generator, double[] weights, Span<int> destination)
    {
        Guard.NotNull(generator, "generator");
        CategoricalProbabilityTable table = new CategoricalProbabilityTable(weights, "weights");
        if (FillDeterministic(table, destination))
        {
            return;
        }

        if (destination.Length == 0)
        {
            return;
        }

        double offset = FloatingUniform.NextDoubleClosedOpen(generator);
        int index = 0;
        while (index < destination.Length)
        {
            double position = (offset + (double)index) / (double)destination.Length;
            destination[index] = FindCategory(table, position);
            index++;
        }
    }

    /// <summary>Returns residual resampling indexes from relative weights.</summary>
    /// <param name="generator">The generator that supplies fractional-residual draws.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="count">The nonnegative output count.</param>
    /// <returns>Exactly count ancestor indexes.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the count is negative, or the weights are empty or invalid.</exception>
    public static int[] ResidualIndexes(IRandomSource generator, double[] weights, int count)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(weights, "weights");
        Guard.Count(count, "count");
        int[] result = new int[count];
        FillResidualIndexes(generator, weights, result);
        return result;
    }

    /// <summary>Fills a destination with residual resampling indexes.</summary>
    /// <param name="generator">The generator that supplies fractional-residual draws.</param>
    /// <param name="weights">The nonempty finite nonnegative relative weights.</param>
    /// <param name="destination">The destination ancestor indexes.</param>
    /// <exception cref="RandomArgumentException">An argument is null or the weights are empty or invalid.</exception>
    public static void FillResidualIndexes(IRandomSource generator, double[] weights, Span<int> destination)
    {
        Guard.NotNull(generator, "generator");
        CategoricalProbabilityTable table = new CategoricalProbabilityTable(weights, "weights");
        if (FillDeterministic(table, destination))
        {
            return;
        }

        double[] residualWeights = new double[table.Length()];
        double residualTotal = 0.0;
        int lastPositiveCategory = -1;
        int outputIndex = 0;
        int category = 0;
        while (category < table.Length())
        {
            double expected = table.NormalizedProbability(category) * (double)destination.Length;
            int deterministicCount = (int)Math.Floor(expected);
            residualWeights[category] = expected - (double)deterministicCount;
            residualTotal += residualWeights[category];
            if (table.NormalizedProbability(category) > 0.0)
            {
                lastPositiveCategory = category;
            }

            int available = destination.Length - outputIndex;
            if (deterministicCount > available)
            {
                deterministicCount = available;
            }

            int copy = 0;
            while (copy < deterministicCount)
            {
                destination[outputIndex] = category;
                outputIndex++;
                copy++;
            }

            category++;
        }

        int remaining = destination.Length - outputIndex;
        if (remaining == 0)
        {
            return;
        }

        if (residualTotal == 0.0)
        {
            residualWeights[lastPositiveCategory] = 1.0;
        }

        CategoricalProbabilityTable residualTable = new CategoricalProbabilityTable(residualWeights, "weights");
        while (outputIndex < destination.Length)
        {
            destination[outputIndex] = FindCategory(residualTable, FloatingUniform.NextDoubleClosedOpen(generator));
            outputIndex++;
        }
    }

    static bool FillDeterministic(CategoricalProbabilityTable table, Span<int> destination)
    {
        int deterministicIndex = table.DeterministicIndex();
        if (deterministicIndex < 0)
        {
            return false;
        }

        destination.Fill(deterministicIndex);
        return true;
    }

    static int FindCategory(CategoricalProbabilityTable table, double position)
    {
        int category = 0;
        while (category < table.Length() - 1 && position >= table.CumulativeProbability(category))
        {
            category++;
        }

        return category;
    }
}
