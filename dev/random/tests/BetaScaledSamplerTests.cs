namespace random.Tests;

public class BetaScaledSamplerTests
{
    [Test]
    public void SamplesMatchOverflowSafeBetaInterpolation()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(42UL, mode);
            RandomGenerator referenceGenerator = Create(42UL, mode);
            BetaScaledSampler actual = new BetaScaledSampler(actualGenerator, 2.0, 5.0, double.MinValue, double.MaxValue);
            BetaSampler reference = new BetaSampler(referenceGenerator, 2.0, 5.0);

            for (int index = 0; index < 100; index++)
            {
                double unit = reference.Sample();
                double expected = double.MinValue * (1.0 - unit) + double.MaxValue * unit;
                double value = actual.Sample();
                Assert.Multiple(() =>
                {
                    Assert.That(PortableMath.DoubleBits(value), Is.EqualTo(PortableMath.DoubleBits(expected)));
                    Assert.That(double.IsFinite(value), Is.True);
                    Assert.That(value, Is.InRange(double.MinValue, double.MaxValue));
                });
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void FillMatchesRepeatedSamples()
    {
        RandomGenerator fillGenerator = Create(123456UL, RandomMathMode.Portable);
        RandomGenerator scalarGenerator = Create(123456UL, RandomMathMode.Portable);
        BetaScaledSampler fillSampler = new BetaScaledSampler(fillGenerator, 0.25, 0.75, -17.0, 91.0);
        BetaScaledSampler scalarSampler = new BetaScaledSampler(scalarGenerator, 0.25, 0.75, -17.0, 91.0);
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
    public void EqualBoundsReturnExactMinimumWithoutConsumption()
    {
        RandomGenerator generator = Create(71UL, RandomMathMode.Portable);
        double negativeZero = PortableMath.DoubleFromBits(PortableMath.DoubleSignMask);
        BetaScaledSampler sampler = new BetaScaledSampler(generator, 0.25, 0.75, negativeZero, 0.0);
        byte[] before = generator.ExportState().ToBytes();
        double[] values = new double[16];

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
    public void RepresentationalBetaEndpointsMapToExactBounds()
    {
        BetaScaledSampler lower = new BetaScaledSampler(Create(42UL, RandomMathMode.Portable), double.Epsilon, double.MaxValue, -10.0, 20.0);
        BetaScaledSampler upper = new BetaScaledSampler(Create(42UL, RandomMathMode.Portable), double.MaxValue, double.Epsilon, -10.0, 20.0);

        Assert.Multiple(() =>
        {
            Assert.That(lower.Sample(), Is.EqualTo(-10.0));
            Assert.That(upper.Sample(), Is.EqualTo(20.0));
        });
    }

    [Test]
    public void InvalidConstructorArgumentsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullError = Assert.Throws<RandomArgumentException>(() => new BetaScaledSampler(NullReference.Value<global::random.IRandomSource>(), 1.0, 1.0, 0.0, 1.0));
        Assert.That(nullError.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new BetaScaledSampler(generator, 0.0, 1.0, 0.0, 1.0)), "alpha" },
            new object[] { (TestDelegate)(() => new BetaScaledSampler(generator, 1.0, 0.0, 0.0, 1.0)), "beta" },
            new object[] { (TestDelegate)(() => new BetaScaledSampler(generator, 1.0, 1.0, double.NaN, 1.0)), "minimum" },
            new object[] { (TestDelegate)(() => new BetaScaledSampler(generator, 1.0, 1.0, 0.0, double.PositiveInfinity)), "maximum" },
            new object[] { (TestDelegate)(() => new BetaScaledSampler(generator, 1.0, 1.0, 2.0, 1.0)), "maximum" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
