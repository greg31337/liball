namespace random.Tests;

public class LogSeriesSamplerTests
{
    [Test]
    public void PortableSamplesMatchKempRejectionExactly()
    {
        double[] probabilities = [0.01, 0.5, 0.95];
        foreach (double probability in probabilities)
        {
            RandomGenerator actualGenerator = Create(101UL, RandomMathMode.Portable);
            RandomGenerator referenceGenerator = Create(101UL, RandomMathMode.Portable);
            byte[] before = actualGenerator.ExportState().ToBytes();
            LogSeriesSampler sampler = new LogSeriesSampler(actualGenerator, probability);

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
            for (int index = 0; index < 2000; index++)
            {
                Assert.That(sampler.Sample(), Is.EqualTo(ReferenceSample(referenceGenerator, probability)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void FillMatchesRepeatedScalarSamplesInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator fillGenerator = Create(202UL, mode);
            RandomGenerator scalarGenerator = Create(202UL, mode);
            LogSeriesSampler fillSampler = new LogSeriesSampler(fillGenerator, 0.875);
            LogSeriesSampler scalarSampler = new LogSeriesSampler(scalarGenerator, 0.875);
            long[] actual = new long[513];
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
                Assert.That(actual, Is.All.GreaterThanOrEqualTo(1L));
                Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void ProbabilityZeroIsAPointMassWithoutConsumption()
    {
        RandomGenerator generator = Create(303UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        LogSeriesSampler sampler = new LogSeriesSampler(generator, 0.0);
        long[] values = new long[32];

        long scalar = sampler.Sample();
        sampler.Fill(values);

        Assert.Multiple(() =>
        {
            Assert.That(scalar, Is.EqualTo(1L));
            Assert.That(values, Is.All.EqualTo(1L));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void LargestRepresentableProbabilityProducesValidCounts()
    {
        LogSeriesSampler sampler = new LogSeriesSampler(
            Create(404UL, RandomMathMode.Portable),
            PortableMath.NextDown(1.0));
        long[] values = new long[10000];

        sampler.Fill(values);

        Assert.That(values, Is.All.GreaterThanOrEqualTo(1L));
    }

    [Test]
    public void SampleMeanMatchesTheLogarithmicSeriesLaw()
    {
        const int sampleCount = 200000;
        const double probability = 0.8;
        LogSeriesSampler sampler = new LogSeriesSampler(Create(505UL, RandomMathMode.Portable), probability);
        double sum = 0.0;
        for (int index = 0; index < sampleCount; index++)
        {
            sum += sampler.Sample();
        }

        double expected = -probability / ((1.0 - probability) * Math.Log(1.0 - probability));
        Assert.That(sum / sampleCount, Is.EqualTo(expected).Within(0.025));
    }

    [Test]
    public void InvalidProbabilitiesAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullGenerator = Assert.Throws<RandomArgumentException>(() => new LogSeriesSampler(NullReference.Value<global::random.IRandomSource>(), 0.5));
        Assert.That(nullGenerator.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(606UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] invalid =
        [
            -double.Epsilon,
            1.0,
            PortableMath.NextUp(1.0),
            double.NaN,
            double.NegativeInfinity,
            double.PositiveInfinity
        ];

        foreach (double probability in invalid)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => new LogSeriesSampler(generator, probability));
            Assert.That(error.ArgumentName(), Is.EqualTo("probability"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static long ReferenceSample(RandomGenerator generator, double probability)
    {
        double complementLog = PortableMath.Log1p(-probability);
        while (true)
        {
            double comparison = generator.NextDoubleOpenOpen();
            if (comparison >= probability)
            {
                return 1L;
            }

            double unit = generator.NextDoubleOpenOpen();
            double quotient = -PortableMath.Expm1(complementLog * unit);
            if (comparison <= quotient * quotient)
            {
                return (long)Math.Floor(1.0 + PortableMath.Log(comparison) / PortableMath.Log(quotient));
            }

            if (comparison >= quotient)
            {
                return 1L;
            }

            return 2L;
        }
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
