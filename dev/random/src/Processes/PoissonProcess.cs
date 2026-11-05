using System;

namespace random;

internal static class PoissonProcess
{
    internal static int RequiredArrivalCapacity(RandomGenerator generator, double rate, double timeHorizon)
    {
        double mean = ValidateParameters(generator, rate, timeHorizon);
        RandomGenerator preview = generator.Clone();
        long count = new PoissonSampler(preview, mean).Sample();
        if (count > int.MaxValue)
        {
            throw RandomExhaustedException.Create("The sampled Poisson arrival count exceeds array capacity.");
        }

        return (int)count;
    }

    internal static int FillArrivals(RandomGenerator generator, double rate, double timeHorizon, Span<double> destination)
    {
        double mean = ValidateParameters(generator, rate, timeHorizon);
        RandomGenerator preview = generator.Clone();
        long previewCount = new PoissonSampler(preview, mean).Sample();
        if (previewCount > int.MaxValue)
        {
            throw RandomExhaustedException.Create("The sampled Poisson arrival count exceeds array capacity.");
        }

        int count = (int)previewCount;
        Guard.SpanLength(destination.Length, count, "destination");
        long actualCount = new PoissonSampler(generator, mean).Sample();
        if (actualCount != previewCount)
        {
            throw RandomStateException.Create("A cloned generator did not replay the Poisson arrival count.");
        }

        double[] arrivals = new double[count];
        int index = 0;
        while (index < count)
        {
            double arrival = FloatingUniform.NextDoubleOpenOpen(generator) * timeHorizon;
            if (arrival >= timeHorizon)
            {
                arrival = Math.BitDecrement(timeHorizon);
            }

            if (arrival < 0.0)
            {
                arrival = 0.0;
            }

            arrivals[index] = arrival;
            index++;
        }

        Array.Sort(arrivals);
        arrivals.CopyTo(destination);
        return count;
    }

    internal static void FillCounts(RandomGenerator generator, double rate, double timeHorizon, int pathCount, int intervalCount, Span<long> destination)
    {
        double totalMean = ValidateParameters(generator, rate, timeHorizon);
        Guard.Count(pathCount, "pathCount");
        Guard.PositiveCount(intervalCount, "intervalCount");
        double intervalMean = totalMean / (double)intervalCount;
        ValidateCountCapacity(generator.Clone(), intervalMean, pathCount, intervalCount);
        PoissonSampler sampler = new PoissonSampler(generator, intervalMean);
        int statesPerPath = intervalCount + 1;
        int path = 0;
        while (path < pathCount)
        {
            int offset = path * statesPerPath;
            long total = 0L;
            destination[offset] = 0L;
            int interval = 1;
            while (interval <= intervalCount)
            {
                total += sampler.Sample();
                destination[offset + interval] = total;
                interval++;
            }

            path++;
        }
    }

    static double ValidateParameters(RandomGenerator generator, double rate, double timeHorizon)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(rate, "rate");
        if (rate < 0.0)
        {
            throw RandomArgumentException.Create("rate", "The Poisson rate must not be negative.");
        }

        Guard.Finite(timeHorizon, "timeHorizon");
        if (timeHorizon < 0.0)
        {
            throw RandomArgumentException.Create("timeHorizon", "The time horizon must not be negative.");
        }

        if (rate == 0.0 || timeHorizon == 0.0)
        {
            return 0.0;
        }

        if (rate > PoissonSampler.MaximumMean / timeHorizon)
        {
            throw RandomArgumentException.Create("rate", "The rate and time horizon produce an unsupported Poisson mean.");
        }

        return rate * timeHorizon;
    }

    static void ValidateCountCapacity(RandomGenerator generator, double intervalMean, int pathCount, int intervalCount)
    {
        PoissonSampler sampler = new PoissonSampler(generator, intervalMean);
        int path = 0;
        while (path < pathCount)
        {
            long total = 0L;
            int interval = 0;
            while (interval < intervalCount)
            {
                long increment = sampler.Sample();
                if (increment > long.MaxValue - total)
                {
                    throw RandomExhaustedException.Create("A cumulative Poisson count exceeds the signed 64-bit range.");
                }

                total += increment;
                interval++;
            }

            path++;
        }
    }
}
