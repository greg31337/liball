namespace random.Tests;

public class ChiSamplerTests
{
    [Test]
    public void SamplesAreExactSelectedModeSquareRootsOfChiSquareSamples()
    {
        double[] degrees = [double.Epsilon, 0.5, 2.0, 10.0, double.MaxValue];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (double degreesOfFreedom in degrees)
        {
            foreach (RandomMathMode mode in modes)
            {
                RandomGenerator actualGenerator = Create(42UL, mode);
                RandomGenerator referenceGenerator = Create(42UL, mode);
                ChiSampler actual = new ChiSampler(actualGenerator, degreesOfFreedom);
                ChiSquareSampler reference = new ChiSquareSampler(referenceGenerator, degreesOfFreedom);

                for (int index = 0; index < 64; index++)
                {
                    double squared = reference.Sample();
                    double expected = mode == RandomMathMode.Portable ? PortableMath.Sqrt(squared) : Math.Sqrt(squared);
                    Assert.That(PortableMath.DoubleBits(actual.Sample()), Is.EqualTo(PortableMath.DoubleBits(expected)));
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void FillMatchesRepeatedSamplesInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator fillGenerator = Create(123456UL, mode);
            RandomGenerator scalarGenerator = Create(123456UL, mode);
            ChiSampler fillSampler = new ChiSampler(fillGenerator, 7.5);
            ChiSampler scalarSampler = new ChiSampler(scalarGenerator, 7.5);
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
    }

    [Test]
    public void InvalidDegreesOfFreedomAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullError = Assert.Throws<RandomArgumentException>(() => new ChiSampler(NullReference.Value<global::random.IRandomSource>(), 1.0));
        Assert.That(nullError.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] invalid = [double.NaN, double.NegativeInfinity, double.PositiveInfinity, 0.0, -double.Epsilon];

        foreach (double degreesOfFreedom in invalid)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => new ChiSampler(generator, degreesOfFreedom));
            Assert.That(error.ArgumentName(), Is.EqualTo("degreesOfFreedom"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void ExtremeDegreesOfFreedomRemainFinite()
    {
        ChiSampler minimum = new ChiSampler(Create(42UL, RandomMathMode.Portable), double.Epsilon);
        ChiSampler maximum = new ChiSampler(Create(42UL, RandomMathMode.Portable), double.MaxValue);
        double expectedMaximum = PortableMath.Sqrt(double.MaxValue);

        for (int index = 0; index < 32; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(PortableMath.DoubleBits(minimum.Sample()), Is.EqualTo(0UL));
                Assert.That(PortableMath.DoubleBits(maximum.Sample()), Is.EqualTo(PortableMath.DoubleBits(expectedMaximum)));
            });
        }
    }

    [Test]
    public void PortableSequenceHasLockedBits()
    {
        ulong[] expected =
        [
            0x400A66BB89675348UL,
            0x4009F6B6122C22B1UL,
            0x400C7CCC2AC5EBFCUL,
            0x400EB54972AADEBAUL
        ];
        ChiSampler sampler = new ChiSampler(Create(42UL, RandomMathMode.Portable), 10.0);

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
