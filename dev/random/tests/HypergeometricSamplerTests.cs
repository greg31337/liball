namespace random.Tests;

public class HypergeometricSamplerTests
{
    [Test]
    public void SparseOrientationsMatchExactSequentialUrnSampling()
    {
        int[][] cases =
        [
            [100, 30, 5, 30, 5, 0, 1],
            [100, 30, 97, 30, 3, 30, -1],
            [100, 4, 50, 50, 4, 0, 1],
            [100, 96, 50, 50, 4, 50, -1]
        ];

        foreach (int[] testCase in cases)
        {
            RandomGenerator actualGenerator = Create(42UL);
            RandomGenerator referenceGenerator = Create(42UL);
            byte[] before = actualGenerator.ExportState().ToBytes();
            HypergeometricSampler sampler = new HypergeometricSampler(
                actualGenerator,
                testCase[0],
                testCase[1],
                testCase[2]);

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
            for (int index = 0; index < 257; index++)
            {
                int marked = SampleUrn(referenceGenerator, testCase[0], testCase[3], testCase[4]);
                int expected = testCase[5] + testCase[6] * marked;
                Assert.That(sampler.Sample(), Is.EqualTo(expected));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void ExactSupportBoundsArePreservedAndReachable()
    {
        HypergeometricSampler sampler = new HypergeometricSampler(Create(123UL), 10, 6, 7);
        bool sawLower = false;
        bool sawUpper = false;

        for (int index = 0; index < 10000; index++)
        {
            int value = sampler.Sample();
            Assert.That(value, Is.InRange(3, 6));
            if (value == 3)
            {
                sawLower = true;
            }

            if (value == 6)
            {
                sawUpper = true;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(sawLower, Is.True);
            Assert.That(sawUpper, Is.True);
        });
    }

    [Test]
    public void DenseModeCenteredSamplingHasExpectedMoments()
    {
        const int population = 200;
        const int successes = 90;
        const int draws = 80;
        const int sampleCount = 150000;
        HypergeometricSampler sampler = new HypergeometricSampler(Create(234UL), population, successes, draws);
        double sum = 0.0;
        double squareSum = 0.0;
        for (int index = 0; index < sampleCount; index++)
        {
            double value = sampler.Sample();
            sum += value;
            squareSum += value * value;
        }

        double actualMean = sum / sampleCount;
        double actualVariance = squareSum / sampleCount - actualMean * actualMean;
        double successFraction = (double)successes / (double)population;
        double expectedMean = (double)draws * successFraction;
        double expectedVariance = (double)draws * successFraction * (1.0 - successFraction);
        expectedVariance *= (double)(population - draws) / (double)(population - 1);
        Assert.Multiple(() =>
        {
            Assert.That(actualMean, Is.EqualTo(expectedMean).Within(0.04));
            Assert.That(actualVariance, Is.EqualTo(expectedVariance).Within(0.2));
        });
    }

    [Test]
    public void DenseSamplingHandlesNonzeroLowerSupportAndMaximumPopulation()
    {
        HypergeometricSampler bounded = new HypergeometricSampler(Create(345UL), 400, 250, 230);
        HypergeometricSampler maximum = new HypergeometricSampler(
            Create(456UL),
            int.MaxValue,
            1073741824,
            1073741824);

        for (int index = 0; index < 128; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(bounded.Sample(), Is.InRange(80, 230));
                Assert.That(maximum.Sample(), Is.InRange(1, 1073741824));
            });
        }
    }

    [Test]
    public void DegenerateSupportsConsumeNoStateIncludingReservoirBits()
    {
        RandomGenerator generator = Create(567UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        HypergeometricSampler empty = new HypergeometricSampler(generator, 0, 0, 0);
        HypergeometricSampler noSuccesses = new HypergeometricSampler(generator, 10, 0, 7);
        HypergeometricSampler allSuccesses = new HypergeometricSampler(generator, 10, 10, 7);
        HypergeometricSampler noDraws = new HypergeometricSampler(generator, 10, 6, 0);
        HypergeometricSampler allDraws = new HypergeometricSampler(generator, 10, 6, 10);
        int[] values = new int[16];

        int emptyValue = empty.Sample();
        int noSuccessValue = noSuccesses.Sample();
        int allSuccessValue = allSuccesses.Sample();
        int noDrawValue = noDraws.Sample();
        allDraws.Fill(values);

        Assert.Multiple(() =>
        {
            Assert.That(emptyValue, Is.Zero);
            Assert.That(noSuccessValue, Is.Zero);
            Assert.That(allSuccessValue, Is.EqualTo(7));
            Assert.That(noDrawValue, Is.Zero);
            Assert.That(values, Is.All.EqualTo(6));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void FillMatchesRepeatedScalarSamplesForSparseAndDenseCases()
    {
        int[][] cases = [[100, 30, 5], [200, 90, 80]];
        foreach (int[] testCase in cases)
        {
            RandomGenerator fillGenerator = Create(678UL);
            RandomGenerator scalarGenerator = Create(678UL);
            HypergeometricSampler fillSampler = new HypergeometricSampler(
                fillGenerator,
                testCase[0],
                testCase[1],
                testCase[2]);
            HypergeometricSampler scalarSampler = new HypergeometricSampler(
                scalarGenerator,
                testCase[0],
                testCase[1],
                testCase[2]);
            int[] actual = new int[257];
            int[] expected = new int[actual.Length];
            byte[] beforeEmpty = fillGenerator.ExportState().ToBytes();

            fillSampler.Fill(Span<int>.Empty);
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(beforeEmpty));
            fillSampler.Fill(actual);
            for (int index = 0; index < expected.Length; index++)
            {
                expected[index] = scalarSampler.Sample();
            }

            Assert.Multiple(() =>
            {
                Assert.That(actual, Is.EqualTo(expected));
                Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void InvalidCountsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullGenerator = Assert.Throws<RandomArgumentException>(() => new HypergeometricSampler(NullReference.Value<global::random.IRandomSource>(), 1, 1, 1));
        Assert.That(nullGenerator.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(789UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new HypergeometricSampler(generator, -1, 0, 0)), "populationSize" },
            new object[] { (TestDelegate)(() => new HypergeometricSampler(generator, 10, -1, 0)), "successCount" },
            new object[] { (TestDelegate)(() => new HypergeometricSampler(generator, 10, 11, 0)), "successCount" },
            new object[] { (TestDelegate)(() => new HypergeometricSampler(generator, 10, 5, -1)), "draws" },
            new object[] { (TestDelegate)(() => new HypergeometricSampler(generator, 10, 5, 11)), "draws" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static int SampleUrn(RandomGenerator generator, int population, int marked, int draws)
    {
        int selectedMarked = 0;
        int remainingPopulation = population;
        int remainingMarked = marked;
        for (int index = 0; index < draws; index++)
        {
            int selected = generator.NextInt32Bounded(remainingPopulation);
            if (selected < remainingMarked)
            {
                selectedMarked++;
                remainingMarked--;
            }

            remainingPopulation--;
        }

        return selectedMarked;
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
