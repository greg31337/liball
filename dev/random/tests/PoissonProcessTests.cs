namespace random.Tests;

public class PoissonProcessTests
{
    [Test]
    public void ArrivalTimesMatchConditionalUniformReferenceExactly()
    {
        const double rate = 3.25;
        const double horizon = 4.0;
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator referenceGenerator = Create(101UL);
        long sampledCount = new PoissonSampler(referenceGenerator, rate * horizon).Sample();
        double[] expected = new double[(int)sampledCount];
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = referenceGenerator.NextDoubleOpenOpen() * horizon;
        }

        Array.Sort(expected);
        double[] actual = RandomProcesses.PoissonArrivals(actualGenerator, rate, horizon);

        AssertExact(actual, expected);
        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.Ordered.Ascending);
            Assert.That(actual, Is.All.InRange(0.0, Math.BitDecrement(horizon)));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void CapacityPreviewDoesNotAdvanceAndSpanLeavesTailUntouched()
    {
        RandomGenerator generator = Create(202UL);
        generator.NextBoolean();
        byte[] beforePreview = generator.ExportState().ToBytes();
        int required = RandomProcesses.RequiredPoissonArrivalCapacity(generator, 4.0, 3.0);
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(beforePreview));
        double[] destination = Enumerable.Repeat(-1.0, required + 5).ToArray();

        int written = RandomProcesses.PoissonArrivals(generator, 4.0, 3.0, destination);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.EqualTo(required));
            Assert.That(destination.Take(written), Is.Ordered.Ascending);
            Assert.That(destination.Take(written), Is.All.InRange(0.0, Math.BitDecrement(3.0)));
            Assert.That(destination.Skip(written), Is.All.EqualTo(-1.0));
        });
    }

    [Test]
    public void InsufficientArrivalCapacityIsAtomic()
    {
        RandomGenerator generator = Create(303UL);
        generator.NextBoolean();
        int required = RandomProcesses.RequiredPoissonArrivalCapacity(generator, 20.0, 5.0);
        Assert.That(required, Is.GreaterThan(0));
        byte[] before = generator.ExportState().ToBytes();
        double[] destination = Enumerable.Repeat(7.0, required - 1).ToArray();

        AssertArgument("destination", () => RandomProcesses.PoissonArrivals(generator, 20.0, 5.0, destination));

        Assert.Multiple(() =>
        {
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(destination, Is.All.EqualTo(7.0));
            Assert.That(RandomProcesses.RequiredPoissonArrivalCapacity(generator, 20.0, 5.0), Is.EqualTo(required));
        });
    }

    [Test]
    public void CumulativeCountPathMatchesIndependentPoissonIncrementsExactly()
    {
        const double rate = 2.75;
        const double horizon = 8.0;
        const int intervalCount = 16;
        RandomGenerator actualGenerator = Create(404UL);
        RandomGenerator referenceGenerator = Create(404UL);
        PoissonSampler sampler = new PoissonSampler(referenceGenerator, rate * horizon / intervalCount);
        long[] expected = new long[intervalCount + 1];
        for (int interval = 1; interval <= intervalCount; interval++)
        {
            expected[interval] = expected[interval - 1] + sampler.Sample();
        }

        long[] actual = RandomProcesses.PoissonCounts(actualGenerator, rate, horizon, intervalCount);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Is.Ordered.Ascending);
            Assert.That(actual[0], Is.Zero);
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void CountBatchIsPathMajorAndMatchesRepeatedPaths()
    {
        const int pathCount = 6;
        const int intervalCount = 11;
        RandomGenerator batchGenerator = Create(505UL);
        RandomGenerator repeatedGenerator = Create(505UL);
        long[] expected = new long[pathCount * (intervalCount + 1)];
        for (int path = 0; path < pathCount; path++)
        {
            RandomProcesses.PoissonCounts(repeatedGenerator, 1.5, 7.0, intervalCount)
                .CopyTo(expected, path * (intervalCount + 1));
        }

        long[] actual = RandomProcesses.PoissonCounts(batchGenerator, 1.5, 7.0, pathCount, intervalCount);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual.Where((value, index) => index % (intervalCount + 1) == 0), Is.All.Zero);
            Assert.That(batchGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void AllocatingAndDestinationCountPathsReplayExactly()
    {
        RandomGenerator allocating = Create(606UL);
        RandomGenerator destinationGenerator = Create(606UL);
        long[] expected = new long[RandomProcesses.PoissonCountBatchOutputLength(4, 13)];

        long[] actual = RandomProcesses.PoissonCounts(allocating, 3.0, 2.0, 4, 13);
        RandomProcesses.PoissonCounts(destinationGenerator, 3.0, 2.0, 4, 13, expected);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(allocating.ExportState().ToBytes(), Is.EqualTo(destinationGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ZeroRateOrHorizonProducesNoEventsWithoutConsumption()
    {
        RandomGenerator generator = Create(707UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        double[] zeroRate = RandomProcesses.PoissonArrivals(generator, 0.0, 100.0);
        double[] zeroHorizon = RandomProcesses.PoissonArrivals(generator, 100.0, 0.0);
        long[] zeroCounts = RandomProcesses.PoissonCounts(generator, 0.0, 100.0, 5, 7);
        long[] zeroHorizonCounts = RandomProcesses.PoissonCounts(generator, 100.0, 0.0, 7);

        Assert.Multiple(() =>
        {
            Assert.That(zeroRate, Is.Empty);
            Assert.That(zeroHorizon, Is.Empty);
            Assert.That(zeroCounts, Is.All.Zero);
            Assert.That(zeroHorizonCounts, Is.All.Zero);
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidParametersCapacityAndStorageAreRejectedAtomically()
    {
        AssertArgument("generator", () => RandomProcesses.PoissonArrivals(NullReference.Value<global::random.RandomGenerator>(), 1.0, 1.0));
        AssertArgument("rate", () => RandomProcesses.PoissonArrivals(Create(801UL), -1.0, 1.0));
        AssertArgument("rate", () => RandomProcesses.PoissonArrivals(Create(802UL), double.NaN, 1.0));
        AssertArgument("timeHorizon", () => RandomProcesses.PoissonArrivals(Create(803UL), 1.0, -1.0));
        AssertArgument("timeHorizon", () => RandomProcesses.PoissonArrivals(Create(804UL), 1.0, double.PositiveInfinity));
        AssertArgument("intervalCount", () => RandomProcesses.PoissonCounts(Create(805UL), 1.0, 1.0, 0));
        AssertArgument("pathCount", () => RandomProcesses.PoissonCountBatchOutputLength(-1, 2));
        AssertArgument("intervalCount", () => RandomProcesses.PoissonCountOutputLength(int.MaxValue));

        RandomGenerator generator = Create(806UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] arrivals = [2.0, 3.0];
        long[] counts = [5L, 7L];
        AssertArgument("rate", () => RandomProcesses.PoissonArrivals(generator, PoissonSampler.MaximumMean, 2.0));
        AssertArgument("destination", () => RandomProcesses.PoissonCounts(generator, 1.0, 1.0, 2, counts));
        Assert.Throws<RandomExhaustedException>(() => RandomProcesses.PoissonArrivals(generator, 1.0e10, 1.0, arrivals));

        Assert.Multiple(() =>
        {
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(arrivals, Is.EqualTo(new double[] { 2.0, 3.0 }));
            Assert.That(counts, Is.EqualTo(new long[] { 5L, 7L }));
            Assert.That(RandomProcesses.PoissonCountOutputLength(7), Is.EqualTo(8));
            Assert.That(RandomProcesses.PoissonCountBatchOutputLength(3, 7), Is.EqualTo(24));
        });
    }

    static void AssertExact(double[] actual, double[] expected)
    {
        Assert.That(actual, Has.Length.EqualTo(expected.Length));
        for (int index = 0; index < actual.Length; index++)
        {
            Assert.That(PortableMath.DoubleBits(actual[index]), Is.EqualTo(PortableMath.DoubleBits(expected[index])), $"index {index}");
        }
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
