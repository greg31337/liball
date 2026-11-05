namespace random.Tests;

public class ZipfSamplerTests
{
    [Test]
    public void PortableSamplesMatchPowerEnvelopeRejection()
    {
        double[] exponents = [1.5, 2.0, 4.0];
        foreach (double exponent in exponents)
        {
            RandomGenerator actualGenerator = Create(707UL, RandomMathMode.Portable);
            RandomGenerator referenceGenerator = Create(707UL, RandomMathMode.Portable);
            byte[] before = actualGenerator.ExportState().ToBytes();
            ZipfSampler sampler = new ZipfSampler(actualGenerator, exponent);

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
            for (int index = 0; index < 2000; index++)
            {
                Assert.That(sampler.Sample(), Is.EqualTo(ReferenceSample(referenceGenerator, exponent)));
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
            RandomGenerator fillGenerator = Create(808UL, mode);
            RandomGenerator scalarGenerator = Create(808UL, mode);
            ZipfSampler fillSampler = new ZipfSampler(fillGenerator, 2.25);
            ZipfSampler scalarSampler = new ZipfSampler(scalarGenerator, 2.25);
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
    public void ExponentImmediatelyAboveOneUsesTheDocumentedOverflowPolicy()
    {
        RandomGenerator generator = Create(909UL, RandomMathMode.Portable);
        byte[] before = generator.ExportState().ToBytes();
        ZipfSampler sampler = new ZipfSampler(generator, PortableMath.NextUp(1.0));

        Assert.Throws<RandomExhaustedException>(() => sampler.Sample());
        Assert.That(generator.ExportState().ToBytes(), Is.Not.EqualTo(before));
    }

    [Test]
    public void VeryLargeFiniteExponentRemainsStable()
    {
        ZipfSampler sampler = new ZipfSampler(Create(1001UL, RandomMathMode.Portable), double.MaxValue);
        long[] values = new long[1000];

        sampler.Fill(values);

        Assert.That(values, Is.All.EqualTo(1L));
    }

    [Test]
    public void SampleMeanMatchesTheZipfLawWhenItExists()
    {
        const int sampleCount = 200000;
        ZipfSampler sampler = new ZipfSampler(Create(1102UL, RandomMathMode.Portable), 3.0);
        double sum = 0.0;
        for (int index = 0; index < sampleCount; index++)
        {
            sum += sampler.Sample();
        }

        const double expected = 1.3684327776202059;
        Assert.That(sum / sampleCount, Is.EqualTo(expected).Within(0.02));
    }

    [Test]
    public void InvalidExponentsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullGenerator = Assert.Throws<RandomArgumentException>(() => new ZipfSampler(NullReference.Value<global::random.IRandomSource>(), 2.0));
        Assert.That(nullGenerator.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(1203UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] invalid =
        [
            1.0,
            PortableMath.NextDown(1.0),
            0.0,
            -1.0,
            double.NaN,
            double.NegativeInfinity,
            double.PositiveInfinity
        ];

        foreach (double exponent in invalid)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => new ZipfSampler(generator, exponent));
            Assert.That(error.ArgumentName(), Is.EqualTo("exponent"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static long ReferenceSample(RandomGenerator generator, double exponent)
    {
        double exponentMinusOne = exponent - 1.0;
        double envelope = PortableMath.Power(2.0, exponentMinusOne);
        while (true)
        {
            double unit = generator.NextDoubleOpenOpen();
            double comparison = generator.NextDoubleOpenOpen();
            double candidate = Math.Floor(PortableMath.Power(unit, -1.0 / exponentMinusOne));
            double power = PortableMath.Power(1.0 + 1.0 / candidate, exponentMinusOne);
            if (comparison * candidate * (power - 1.0) / (envelope - 1.0) <= power / envelope)
            {
                return (long)candidate;
            }
        }
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
