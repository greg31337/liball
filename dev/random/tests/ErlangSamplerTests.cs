namespace random.Tests;

public class ErlangSamplerTests
{
    [Test]
    public void ErlangMatchesIntegerShapeGammaInBothMathModes()
    {
        int[] shapes = [1, 7, int.MaxValue];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (int shape in shapes)
        {
            foreach (RandomMathMode mode in modes)
            {
                RandomGenerator erlangGenerator = Create(42UL, mode);
                RandomGenerator gammaGenerator = Create(42UL, mode);
                ErlangSampler erlang = new ErlangSampler(erlangGenerator, shape, 1.5);
                GammaSampler gamma = new GammaSampler(gammaGenerator, (double)shape, 1.5);

                for (int index = 0; index < 64; index++)
                {
                    Assert.That(PortableMath.DoubleBits(erlang.Sample()), Is.EqualTo(PortableMath.DoubleBits(gamma.Sample())));
                }

                Assert.That(erlangGenerator.ExportState().ToBytes(), Is.EqualTo(gammaGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void UnitScaleConstructorMatchesExplicitScale()
    {
        RandomGenerator unitGenerator = Create(11UL, RandomMathMode.Portable);
        RandomGenerator explicitGenerator = Create(11UL, RandomMathMode.Portable);
        byte[] before = unitGenerator.ExportState().ToBytes();
        ErlangSampler unit = new ErlangSampler(unitGenerator, 4, 1.0);
        ErlangSampler explicitSampler = new ErlangSampler(explicitGenerator, 4, 1.0);

        Assert.That(unitGenerator.ExportState().ToBytes(), Is.EqualTo(before));

        for (int index = 0; index < 100; index++)
        {
            Assert.That(PortableMath.DoubleBits(unit.Sample()), Is.EqualTo(PortableMath.DoubleBits(explicitSampler.Sample())));
        }

        Assert.That(unitGenerator.ExportState().ToBytes(), Is.EqualTo(explicitGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void FillMatchesRepeatedSamples()
    {
        RandomGenerator fillGenerator = Create(123456UL, RandomMathMode.Portable);
        RandomGenerator scalarGenerator = Create(123456UL, RandomMathMode.Portable);
        ErlangSampler fillSampler = new ErlangSampler(fillGenerator, 9, 0.75);
        ErlangSampler scalarSampler = new ErlangSampler(scalarGenerator, 9, 0.75);
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
    public void ZeroScaleReturnsPositiveZeroWithoutConsumption()
    {
        RandomGenerator generator = Create(71UL, RandomMathMode.Portable);
        ErlangSampler sampler = new ErlangSampler(generator, int.MaxValue, -0.0);
        byte[] before = generator.ExportState().ToBytes();
        double[] values = new double[16];

        double scalar = sampler.Sample();
        sampler.Fill(values);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(scalar), Is.EqualTo(0UL));
            Assert.That(values, Is.All.EqualTo(0.0));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidConstructorArgumentsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullUnit = Assert.Throws<RandomArgumentException>(() => new ErlangSampler(NullReference.Value<global::random.IRandomSource>(), 1, 1.0));
        RandomArgumentException nullScaled = Assert.Throws<RandomArgumentException>(() => new ErlangSampler(NullReference.Value<global::random.IRandomSource>(), 1, 1.0));
        Assert.Multiple(() =>
        {
            Assert.That(nullUnit.ArgumentName(), Is.EqualTo("generator"));
            Assert.That(nullScaled.ArgumentName(), Is.EqualTo("generator"));
        });

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new ErlangSampler(generator, 0, 1.0)), "shape" },
            new object[] { (TestDelegate)(() => new ErlangSampler(generator, -1, 1.0)), "shape" },
            new object[] { (TestDelegate)(() => new ErlangSampler(generator, 1, double.NaN)), "scale" },
            new object[] { (TestDelegate)(() => new ErlangSampler(generator, 1, double.PositiveInfinity)), "scale" },
            new object[] { (TestDelegate)(() => new ErlangSampler(generator, 1, -double.Epsilon)), "scale" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void MaximumIntegerShapeRemainsFiniteAndVariable()
    {
        const double expectedMean = int.MaxValue;
        const double maximumDeviation = 500000.0;
        ErlangSampler sampler = new ErlangSampler(Create(42UL, RandomMathMode.Portable), int.MaxValue, 1.0);
        double first = sampler.Sample();
        bool foundDifferent = false;

        Assert.That(first, Is.EqualTo(expectedMean).Within(maximumDeviation));
        for (int index = 0; index < 64; index++)
        {
            double value = sampler.Sample();
            Assert.Multiple(() =>
            {
                Assert.That(double.IsFinite(value), Is.True);
                Assert.That(value, Is.GreaterThan(0.0));
                Assert.That(value, Is.EqualTo(expectedMean).Within(maximumDeviation));
            });
            if (value != first)
            {
                foundDifferent = true;
            }
        }

        Assert.That(foundDifferent, Is.True);
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
