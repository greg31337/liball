namespace random.Tests;

public class ChiSquareSamplerTests
{
    [Test]
    public void SamplesMatchTheValidatedGammaRelationship()
    {
        double[] degrees = [double.Epsilon, 0.5, 2.0, 10.0, double.MaxValue];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (double degreesOfFreedom in degrees)
        {
            foreach (RandomMathMode mode in modes)
            {
                RandomGenerator actualGenerator = Create(42UL, mode);
                RandomGenerator referenceGenerator = Create(42UL, mode);
                ChiSquareSampler actual = new ChiSquareSampler(actualGenerator, degreesOfFreedom);
                double gammaShape = degreesOfFreedom * 0.5;
                if (gammaShape == 0.0)
                {
                    gammaShape = double.Epsilon;
                }

                GammaSampler reference = new GammaSampler(referenceGenerator, gammaShape, 2.0);
                for (int index = 0; index < 64; index++)
                {
                    Assert.That(PortableMath.DoubleBits(actual.Sample()), Is.EqualTo(PortableMath.DoubleBits(reference.Sample())));
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
        ChiSquareSampler fillSampler = new ChiSquareSampler(fillGenerator, 7.5);
        ChiSquareSampler scalarSampler = new ChiSquareSampler(scalarGenerator, 7.5);
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
        RandomArgumentException nullError = Assert.Throws<RandomArgumentException>(() => new ChiSquareSampler(NullReference.Value<global::random.IRandomSource>(), 1.0));
        Assert.That(nullError.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] invalid = [double.NaN, double.NegativeInfinity, double.PositiveInfinity, 0.0, -double.Epsilon];

        foreach (double degreesOfFreedom in invalid)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => new ChiSquareSampler(generator, degreesOfFreedom));
            Assert.That(error.ArgumentName(), Is.EqualTo("degreesOfFreedom"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void ExtremeDegreesOfFreedomRemainDefined()
    {
        RandomGenerator minimumGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator maximumGenerator = Create(42UL, RandomMathMode.Portable);
        ChiSquareSampler minimum = new ChiSquareSampler(minimumGenerator, double.Epsilon);
        ChiSquareSampler maximum = new ChiSquareSampler(maximumGenerator, double.MaxValue);

        for (int index = 0; index < 32; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(PortableMath.DoubleBits(minimum.Sample()), Is.EqualTo(0UL));
                Assert.That(PortableMath.DoubleBits(maximum.Sample()), Is.EqualTo(PortableMath.DoubleBits(double.MaxValue)));
            });
        }
    }

    [Test]
    public void DeterministicMeansMatchDegreesOfFreedomAcrossRegimes()
    {
        double[] degrees = [0.5, 2.0, 10.0, 100.0];
        double[] tolerances = [0.02, 0.05, 0.15, 0.7];
        const int sampleCount = 100000;

        for (int degreeIndex = 0; degreeIndex < degrees.Length; degreeIndex++)
        {
            double degreesOfFreedom = degrees[degreeIndex];
            ChiSquareSampler sampler = new ChiSquareSampler(Create(123UL, RandomMathMode.Portable), degreesOfFreedom);
            double sum = 0.0;
            for (int index = 0; index < sampleCount; index++)
            {
                double value = sampler.Sample();
                Assert.That(value, Is.GreaterThanOrEqualTo(0.0));
                sum += value;
            }

            Assert.That(sum / sampleCount, Is.EqualTo(degreesOfFreedom).Within(tolerances[degreeIndex]));
        }
    }

    [Test]
    public void PortableSequenceHasLockedBits()
    {
        ulong[] expected =
        [
            0x4025C83A8F1EEA3EUL,
            0x402510EA8FC84B8BUL,
            0x40295C4BFE3C7C72UL,
            0x402D77ECBE8A235BUL
        ];
        ChiSquareSampler sampler = new ChiSquareSampler(Create(42UL, RandomMathMode.Portable), 10.0);

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
