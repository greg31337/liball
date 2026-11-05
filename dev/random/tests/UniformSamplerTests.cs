namespace random.Tests;

public class UniformSamplerTests
{
    [Test]
    public void SampleMatchesTheConfiguredGeneratorRangeExactly()
    {
        RandomGenerator actualGenerator = Create();
        RandomGenerator referenceGenerator = Create();
        byte[] before = actualGenerator.ExportState().ToBytes();
        UniformSampler sampler = new UniformSampler(actualGenerator, -1.0e200, 7.0e199);

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));

        for (int index = 0; index < 100; index++)
        {
            double actual = sampler.Sample();
            double expected = referenceGenerator.NextDoubleInRange(-1.0e200, 7.0e199);
            Assert.That(PortableMath.DoubleBits(actual), Is.EqualTo(PortableMath.DoubleBits(expected)));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void FillMatchesRepeatedScalarSamplesExactly()
    {
        RandomGenerator fillGenerator = Create();
        RandomGenerator scalarGenerator = Create();
        UniformSampler fillSampler = new UniformSampler(fillGenerator, -37.25, 91.5);
        UniformSampler scalarSampler = new UniformSampler(scalarGenerator, -37.25, 91.5);
        double[] actual = new double[257];
        double[] expected = new double[actual.Length];

        fillSampler.Fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = scalarSampler.Sample();
        }

        Assert.Multiple(() =>
        {
            for (int index = 0; index < actual.Length; index++)
            {
                Assert.That(PortableMath.DoubleBits(actual[index]), Is.EqualTo(PortableMath.DoubleBits(expected[index])));
            }

            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void EmptyFillDoesNotConsumeGeneratorState()
    {
        RandomGenerator generator = Create();
        UniformSampler sampler = new UniformSampler(generator, -1.0, 1.0);
        byte[] before = generator.ExportState().ToBytes();

        sampler.Fill(Span<double>.Empty);

        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void EqualBoundsReturnTheExactMinimumWithoutConsumption()
    {
        RandomGenerator generator = Create();
        double negativeZero = PortableMath.DoubleFromBits(PortableMath.DoubleSignMask);
        UniformSampler sampler = new UniformSampler(generator, negativeZero, 0.0);
        byte[] before = generator.ExportState().ToBytes();
        double[] values = new double[8];

        double scalar = sampler.Sample();
        sampler.Fill(values);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(scalar), Is.EqualTo(PortableMath.DoubleSignMask));
            for (int index = 0; index < values.Length; index++)
            {
                Assert.That(PortableMath.DoubleBits(values[index]), Is.EqualTo(PortableMath.DoubleSignMask));
            }

            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidConstructorArgumentsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullError = Assert.Throws<RandomArgumentException>(() => new UniformSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0));
        Assert.That(nullError.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create();
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new UniformSampler(generator, double.NaN, 1.0)), "minimum" },
            new object[] { (TestDelegate)(() => new UniformSampler(generator, double.NegativeInfinity, 1.0)), "minimum" },
            new object[] { (TestDelegate)(() => new UniformSampler(generator, 0.0, double.NaN)), "maximum" },
            new object[] { (TestDelegate)(() => new UniformSampler(generator, 0.0, double.PositiveInfinity)), "maximum" },
            new object[] { (TestDelegate)(() => new UniformSampler(generator, 2.0, 1.0)), "maximum" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void FullFiniteRangeSamplesRemainFiniteAndExcludeTheMaximum()
    {
        UniformSampler sampler = new UniformSampler(Create(), double.MinValue, double.MaxValue);

        for (int index = 0; index < 10000; index++)
        {
            double value = sampler.Sample();
            Assert.Multiple(() =>
            {
                Assert.That(double.IsFinite(value), Is.True);
                Assert.That(value, Is.GreaterThanOrEqualTo(double.MinValue));
                Assert.That(value, Is.LessThan(double.MaxValue));
            });
        }
    }

    static RandomGenerator Create()
    {
        return RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
    }
}
