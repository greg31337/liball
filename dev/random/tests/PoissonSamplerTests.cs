namespace random.Tests;

public class PoissonSamplerTests
{
    [Test]
    public void PublicSamplerMatchesTheSharedPoissonCoreAcrossRegimes()
    {
        double[] means = [0.0, 2.5, 9.999, 10.0, 25.0, 1000000.0];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            foreach (double mean in means)
            {
                RandomGenerator actualGenerator = Create(42UL, mode);
                RandomGenerator referenceGenerator = Create(42UL, mode);
                byte[] before = actualGenerator.ExportState().ToBytes();
                PoissonSampler actual = new PoissonSampler(actualGenerator, mean);
                PoissonCountSampler reference = new PoissonCountSampler(referenceGenerator, mean);

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
                for (int index = 0; index < 257; index++)
                {
                    Assert.That(actual.Sample(), Is.EqualTo(reference.Sample()));
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void FillMatchesRepeatedScalarSamplesAcrossRegimesAndModes()
    {
        double[] means = [2.5, 10.0, 1000.0];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            foreach (double mean in means)
            {
                RandomGenerator fillGenerator = Create(123UL, mode);
                RandomGenerator scalarGenerator = Create(123UL, mode);
                PoissonSampler fillSampler = new PoissonSampler(fillGenerator, mean);
                PoissonSampler scalarSampler = new PoissonSampler(scalarGenerator, mean);
                long[] actual = new long[257];
                long[] expected = new long[actual.Length];
                byte[] beforeEmpty = fillGenerator.ExportState().ToBytes();

                fillSampler.Fill(Span<long>.Empty);
                Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(beforeEmpty));
                fillSampler.Fill(actual);
                for (int index = 0; index < expected.Length; index++)
                {
                    expected[index] = scalarSampler.Sample();
                }

                Assert.Multiple(() =>
                {
                    Assert.That(actual, Is.EqualTo(expected));
                    Assert.That(actual, Is.All.GreaterThanOrEqualTo(0L));
                    Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
                });
            }
        }
    }

    [Test]
    public void ZeroMeanConsumesNoStateIncludingReservoirBits()
    {
        RandomGenerator generator = Create(234UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        PoissonSampler sampler = new PoissonSampler(generator, 0.0);
        long[] values = new long[32];

        long scalar = sampler.Sample();
        sampler.Fill(values);

        Assert.Multiple(() =>
        {
            Assert.That(scalar, Is.Zero);
            Assert.That(values, Is.All.Zero);
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void MaximumSupportedMeanProducesRepresentableCounts()
    {
        PoissonSampler sampler = new PoissonSampler(
            Create(345UL, RandomMathMode.Portable),
            PoissonSampler.MaximumMean);

        Assert.That(PoissonSampler.MaximumMean, Is.EqualTo(PoissonCountSampler.MaximumMean));
        for (int index = 0; index < 1000; index++)
        {
            Assert.That(sampler.Sample(), Is.InRange(0L, long.MaxValue));
        }
    }

    [Test]
    public void InversionAndTransformedRejectionHaveExpectedMoments()
    {
        AssertMoments(2.5, 150000, 0.03, 0.12);
        AssertMoments(25.0, 150000, 0.06, 0.4);
        AssertMoments(1000.0, 150000, 0.2, 3.0);
    }

    [Test]
    public void InvalidMeansAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullGenerator = Assert.Throws<RandomArgumentException>(() => new PoissonSampler(NullReference.Value<global::random.IRandomSource>(), 1.0));
        Assert.That(nullGenerator.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(456UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] invalid =
        [
            -double.Epsilon,
            double.NaN,
            double.NegativeInfinity,
            double.PositiveInfinity,
            Math.BitIncrement(PoissonSampler.MaximumMean)
        ];

        foreach (double mean in invalid)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => new PoissonSampler(generator, mean));
            Assert.That(error.ArgumentName(), Is.EqualTo("mean"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static void AssertMoments(double mean, int sampleCount, double meanTolerance, double varianceTolerance)
    {
        PoissonSampler sampler = new PoissonSampler(Create(567UL, RandomMathMode.Portable), mean);
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
        Assert.Multiple(() =>
        {
            Assert.That(actualMean, Is.EqualTo(mean).Within(meanTolerance));
            Assert.That(actualVariance, Is.EqualTo(mean).Within(varianceTolerance));
        });
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
