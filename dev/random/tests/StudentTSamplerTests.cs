namespace random.Tests;

public class StudentTSamplerTests
{
    [Test]
    public void SamplesMatchNormalAndChiSquareCompositionInBothMathModes()
    {
        double[] degrees = [0.5, 2.0, 10.0, double.MaxValue];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (double degreesOfFreedom in degrees)
        {
            foreach (RandomMathMode mode in modes)
            {
                RandomGenerator actualGenerator = Create(42UL, mode);
                RandomGenerator referenceGenerator = Create(42UL, mode);
                StudentTSampler actual = new StudentTSampler(actualGenerator, degreesOfFreedom);
                NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);
                ChiSquareSampler square = new ChiSquareSampler(referenceGenerator, degreesOfFreedom);

                for (int index = 0; index < 64; index++)
                {
                    double numerator = normal.Sample();
                    double normalized = square.Sample() / degreesOfFreedom;
                    double denominator = mode == RandomMathMode.Portable ? PortableMath.Sqrt(normalized) : Math.Sqrt(normalized);
                    double expected;
                    if (denominator == 0.0)
                    {
                        expected = numerator == 0.0 ? numerator : numerator < 0.0 ? double.NegativeInfinity : double.PositiveInfinity;
                    }
                    else
                    {
                        expected = numerator / denominator;
                    }

                    Assert.That(PortableMath.DoubleBits(actual.Sample()), Is.EqualTo(PortableMath.DoubleBits(expected)));
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void FillMatchesRepeatedSamples()
    {
        RandomGenerator fillGenerator = Create(123456UL, RandomMathMode.Portable);
        RandomGenerator scalarGenerator = Create(123456UL, RandomMathMode.Portable);
        StudentTSampler fillSampler = new StudentTSampler(fillGenerator, 7.5);
        StudentTSampler scalarSampler = new StudentTSampler(scalarGenerator, 7.5);
        double[] actual = new double[257];
        double[] expected = new double[actual.Length];
        byte[] beforeEmptyFill = fillGenerator.ExportState().ToBytes();

        fillSampler.Fill(Span<double>.Empty);
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(beforeEmptyFill));
        fillSampler.Fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = scalarSampler.Sample();
        }

        for (int index = 0; index < actual.Length; index++)
        {
            Assert.That(PortableMath.DoubleBits(actual[index]), Is.EqualTo(PortableMath.DoubleBits(expected[index])));
        }

        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void InvalidDegreesOfFreedomAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullError = Assert.Throws<RandomArgumentException>(() => new StudentTSampler(NullReference.Value<global::random.IRandomSource>(), 1.0));
        Assert.That(nullError.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] invalid = [double.NaN, double.NegativeInfinity, double.PositiveInfinity, 0.0, -double.Epsilon];

        foreach (double degreesOfFreedom in invalid)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => new StudentTSampler(generator, degreesOfFreedom));
            Assert.That(error.ArgumentName(), Is.EqualTo("degreesOfFreedom"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void UnderflowedDenominatorsProduceSignedTailsWithoutNaN()
    {
        StudentTSampler sampler = new StudentTSampler(Create(123UL, RandomMathMode.Portable), double.Epsilon);
        bool foundPositive = false;
        bool foundNegative = false;

        for (int index = 0; index < 256; index++)
        {
            double value = sampler.Sample();
            Assert.That(double.IsNaN(value), Is.False);
            if (value == double.PositiveInfinity)
            {
                foundPositive = true;
            }
            else if (value == double.NegativeInfinity)
            {
                foundNegative = true;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(foundPositive, Is.True);
            Assert.That(foundNegative, Is.True);
        });
    }

    [Test]
    public void MaximumDegreesConvergeToFiniteNormalResolution()
    {
        StudentTSampler sampler = new StudentTSampler(Create(42UL, RandomMathMode.Portable), double.MaxValue);
        double[] expected = [0.3421632852690972, 0.1000382936990038, 1.0653412264348183, 0.41839940324068703];

        foreach (double expectedValue in expected)
        {
            Assert.That(PortableMath.DoubleBits(sampler.Sample()), Is.EqualTo(PortableMath.DoubleBits(expectedValue)));
        }
    }

    [Test]
    public void DeterministicSampleIsCenteredAndSymmetric()
    {
        const int sampleCount = 200000;
        StudentTSampler sampler = new StudentTSampler(Create(123UL, RandomMathMode.Portable), 10.0);
        double sum = 0.0;
        int positiveCount = 0;

        for (int index = 0; index < sampleCount; index++)
        {
            double value = sampler.Sample();
            sum += value;
            if (value > 0.0)
            {
                positiveCount++;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(sum / sampleCount, Is.EqualTo(0.0).Within(0.015));
            Assert.That(positiveCount, Is.InRange(97000, 103000));
        });
    }

    [Test]
    public void PortableSequenceHasLockedBits()
    {
        ulong[] expected =
        [
            0x3FD8183C18A5ED24UL,
            0x3FB6BE21A2EAB9CEUL,
            0x3FF5EF92E8B4F78EUL,
            0x3FD49A888E8E4E64UL
        ];
        StudentTSampler sampler = new StudentTSampler(Create(42UL, RandomMathMode.Portable), 10.0);

        foreach (ulong expectedBits in expected)
        {
            Assert.That(PortableMath.DoubleBits(sampler.Sample()), Is.EqualTo(expectedBits));
        }
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
