using System;

namespace random;

/// <summary>
/// Generates signed 32-bit success counts from a hypergeometric distribution.
/// </summary>
/// <remarks>
/// Sampling is without replacement from a finite population. Sparse cases use an exact
/// sequential urn algorithm after applying draw, omission, success, and failure symmetries
/// to minimize work. Dense cases use mode-centered inversion with adjacent probability-mass
/// ratios, avoiding factorial overflow and tail underflow. The sampler retains the supplied
/// mutable generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class HypergeometricSampler : IHypergeometricSampler
{
    const int SequentialLimit = 64;
    IRandomSource generator;
    int populationSize;
    int successCount;
    int draws;
    int lowerSupport;
    int upperSupport;
    bool sequential;
    int sequentialDraws;
    int sequentialMarked;
    int outputOffset;
    int outputSign;
    int mode;
    double modeProbability;

    /// <summary>
    /// Creates a hypergeometric sampler for a finite population.
    /// </summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="populationSize">The nonnegative population size.</param>
    /// <param name="successCount">The number of population members marked as successes.</param>
    /// <param name="draws">The number of members drawn without replacement.</param>
    /// <remarks>
    /// Successes and draws must each be between zero and the population size inclusive.
    /// A one-point support returns its sole count without consuming random state.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, a count is negative, or successes or draws exceed the population size.</exception>
    public HypergeometricSampler(IRandomSource generator, int populationSize, int successCount, int draws)
    {
        Guard.NotNull(generator, "generator");
        Guard.Count(populationSize, "populationSize");
        Guard.Count(successCount, "successCount");
        if (successCount > populationSize)
        {
            throw RandomArgumentException.Create("successCount", "The success count must not exceed the population size.");
        }

        Guard.Count(draws, "draws");
        if (draws > populationSize)
        {
            throw RandomArgumentException.Create("draws", "The draw count must not exceed the population size.");
        }

        this.generator = generator;
        this.populationSize = populationSize;
        this.successCount = successCount;
        this.draws = draws;
        int failures = populationSize - successCount;
        lowerSupport = draws - failures;
        if (lowerSupport < 0)
        {
            lowerSupport = 0;
        }

        upperSupport = draws < successCount ? draws : successCount;
        sequential = false;
        sequentialDraws = 0;
        sequentialMarked = 0;
        outputOffset = 0;
        outputSign = 1;
        mode = lowerSupport;
        modeProbability = 1.0;
        if (lowerSupport == upperSupport)
        {
            return;
        }

        int omittedDraws = populationSize - draws;
        int minimumDimension = draws;
        int orientation = 0;
        if (omittedDraws < minimumDimension)
        {
            minimumDimension = omittedDraws;
            orientation = 1;
        }

        if (successCount < minimumDimension)
        {
            minimumDimension = successCount;
            orientation = 2;
        }

        if (failures < minimumDimension)
        {
            minimumDimension = failures;
            orientation = 3;
        }

        if (minimumDimension <= SequentialLimit)
        {
            sequential = true;
            ConfigureSequentialPath(orientation, minimumDimension);
            return;
        }

        long numerator = ((long)draws + 1L) * ((long)successCount + 1L);
        mode = (int)(numerator / ((long)populationSize + 2L));
        if (mode < lowerSupport)
        {
            mode = lowerSupport;
        }

        if (mode > upperSupport)
        {
            mode = upperSupport;
        }

        modeProbability = 1.0 / RelativeMassSum();
    }

    /// <summary>
    /// Returns one success count from this sampler's hypergeometric distribution.
    /// </summary>
    /// <returns>An integer from the exact lower support bound through the exact upper support bound.</returns>
    public int Sample()
    {
        if (lowerSupport == upperSupport)
        {
            return lowerSupport;
        }

        if (sequential)
        {
            return SampleSequential();
        }

        return SampleModeCentered();
    }

    /// <summary>
    /// Fills a destination with success counts from this sampler's hypergeometric distribution.
    /// </summary>
    /// <param name="destination">The destination success counts.</param>
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

    void ConfigureSequentialPath(int orientation, int minimumDimension)
    {
        sequentialDraws = minimumDimension;
        if (orientation == 0)
        {
            sequentialMarked = successCount;
            outputOffset = 0;
            outputSign = 1;
        }
        else if (orientation == 1)
        {
            sequentialMarked = successCount;
            outputOffset = successCount;
            outputSign = -1;
        }
        else if (orientation == 2)
        {
            sequentialMarked = draws;
            outputOffset = 0;
            outputSign = 1;
        }
        else
        {
            sequentialMarked = draws;
            outputOffset = draws;
            outputSign = -1;
        }
    }

    int SampleSequential()
    {
        int remainingPopulation = populationSize;
        int remainingMarked = sequentialMarked;
        int markedDraws = 0;
        int index = 0;
        while (index < sequentialDraws)
        {
            int selected = IntegerUniform.NextInt32(generator, remainingPopulation);
            if (selected < remainingMarked)
            {
                markedDraws++;
                remainingMarked--;
            }

            remainingPopulation--;
            index++;
        }

        return outputOffset + outputSign * markedDraws;
    }

    int SampleModeCentered()
    {
        double remaining = FloatingUniform.NextDoubleClosedOpen(generator);
        if (remaining < modeProbability)
        {
            return mode;
        }

        remaining -= modeProbability;
        int left = mode;
        int right = mode;
        double leftRelative = 1.0;
        double rightRelative = 1.0;
        while (left > lowerSupport || right < upperSupport)
        {
            if (left > lowerSupport)
            {
                leftRelative *= LeftRatio(left);
                left--;
                double probability = leftRelative * modeProbability;
                if (remaining < probability)
                {
                    return left;
                }

                remaining -= probability;
            }

            if (right < upperSupport)
            {
                rightRelative *= RightRatio(right);
                right++;
                double probability = rightRelative * modeProbability;
                if (remaining < probability)
                {
                    return right;
                }

                remaining -= probability;
            }

            if (leftRelative == 0.0 && rightRelative == 0.0)
            {
                break;
            }
        }

        return upperSupport;
    }

    double RelativeMassSum()
    {
        double sum = 1.0;
        double compensation = 0.0;
        int left = mode;
        int right = mode;
        double leftRelative = 1.0;
        double rightRelative = 1.0;
        while (left > lowerSupport || right < upperSupport)
        {
            bool changed = false;
            if (left > lowerSupport)
            {
                leftRelative *= LeftRatio(left);
                left--;
                double adjusted = leftRelative - compensation;
                double next = sum + adjusted;
                compensation = (next - sum) - adjusted;
                if (next != sum)
                {
                    changed = true;
                }

                sum = next;
            }

            if (right < upperSupport)
            {
                rightRelative *= RightRatio(right);
                right++;
                double adjusted = rightRelative - compensation;
                double next = sum + adjusted;
                compensation = (next - sum) - adjusted;
                if (next != sum)
                {
                    changed = true;
                }

                sum = next;
            }

            if (!changed || leftRelative == 0.0 && rightRelative == 0.0)
            {
                break;
            }
        }

        return sum;
    }

    double LeftRatio(int count)
    {
        double numerator = (double)count * (double)(populationSize - successCount - draws + count);
        double denominator = (double)(successCount - count + 1) * (double)(draws - count + 1);
        return numerator / denominator;
    }

    double RightRatio(int count)
    {
        double numerator = (double)(successCount - count) * (double)(draws - count);
        double denominator = (double)(count + 1) * (double)(populationSize - successCount - draws + count + 1);
        return numerator / denominator;
    }
}
