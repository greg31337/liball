namespace random.Tests;

public class ExponentialSamplerTests
{
    const ulong ZeroOutputSeed = 0x61C8864680B583EBUL;
    const ulong OneOutputSeed = 0xF8364607E9C949BDUL;
    const ulong UInt64MaximumOutputSeed = 0x31628AF67B2131ABUL;

    [Test]
    public void StandardConstructorMatchesExplicitUnitScale()
    {
        RandomGenerator standardGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator explicitGenerator = Create(42UL, RandomMathMode.Portable);
        byte[] before = standardGenerator.ExportState().ToBytes();
        ExponentialSampler standard = new ExponentialSampler(standardGenerator, 1.0);
        ExponentialSampler explicitSampler = new ExponentialSampler(explicitGenerator, 1.0);

        Assert.That(standardGenerator.ExportState().ToBytes(), Is.EqualTo(before));

        for (int index = 0; index < 100; index++)
        {
            Assert.That(PortableMath.DoubleBits(standard.Sample()), Is.EqualTo(PortableMath.DoubleBits(explicitSampler.Sample())));
        }

        Assert.That(standardGenerator.ExportState().ToBytes(), Is.EqualTo(explicitGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void SamplesTransformOpenUniformValuesInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(987654UL, mode);
            RandomGenerator referenceGenerator = Create(987654UL, mode);
            ExponentialSampler sampler = new ExponentialSampler(actualGenerator, 2.75);

            for (int index = 0; index < 500; index++)
            {
                double unit = referenceGenerator.NextDoubleOpenOpen();
                double logarithm = mode == RandomMathMode.Portable ? PortableMath.Log(unit) : Math.Log(unit);
                double expected = 2.75 * -logarithm;
                double actual = sampler.Sample();
                Assert.Multiple(() =>
                {
                    Assert.That(PortableMath.DoubleBits(actual), Is.EqualTo(PortableMath.DoubleBits(expected)));
                    Assert.That(actual, Is.GreaterThan(0.0));
                });
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
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
            ExponentialSampler fillSampler = new ExponentialSampler(fillGenerator, 3.5);
            ExponentialSampler scalarSampler = new ExponentialSampler(scalarGenerator, 3.5);
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
    public void ZeroScaleReturnsPositiveZeroWithoutConsumption()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator generator = Create(71UL, mode);
            ExponentialSampler sampler = new ExponentialSampler(generator, -0.0);
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
    }

    [Test]
    public void InvalidConstructorArgumentsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullStandard = Assert.Throws<RandomArgumentException>(() => new ExponentialSampler(NullReference.Value<global::random.IRandomSource>(), 1.0));
        RandomArgumentException nullScaled = Assert.Throws<RandomArgumentException>(() => new ExponentialSampler(NullReference.Value<global::random.IRandomSource>(), 1.0));
        Assert.Multiple(() =>
        {
            Assert.That(nullStandard.ArgumentName(), Is.EqualTo("generator"));
            Assert.That(nullScaled.ArgumentName(), Is.EqualTo("generator"));
        });

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        double[] invalidScales = [double.NaN, double.NegativeInfinity, double.PositiveInfinity, -double.Epsilon];

        foreach (double scale in invalidScales)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => new ExponentialSampler(generator, scale));
            Assert.That(error.ArgumentName(), Is.EqualTo("scale"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void OpenIntervalRejectsAZeroNumerator()
    {
        RandomGenerator actualGenerator = Create(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator referenceGenerator = Create(ZeroOutputSeed, RandomMathMode.Portable);
        ExponentialSampler sampler = new ExponentialSampler(actualGenerator, 1.0);
        double unit = referenceGenerator.NextDoubleOpenOpen();
        double expected = -PortableMath.Log(unit);

        double actual = sampler.Sample();

        Assert.Multiple(() =>
        {
            Assert.That(unit, Is.GreaterThan(0.0).And.LessThan(1.0));
            Assert.That(PortableMath.DoubleBits(actual), Is.EqualTo(PortableMath.DoubleBits(expected)));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ExtremeOpenInputsProduceFinitePositiveEndpoints()
    {
        double portableTail = new ExponentialSampler(Create(OneOutputSeed, RandomMathMode.Portable), 1.0).Sample();
        double systemTail = new ExponentialSampler(Create(OneOutputSeed, RandomMathMode.System), 1.0).Sample();
        double portableNearZero = new ExponentialSampler(Create(UInt64MaximumOutputSeed, RandomMathMode.Portable), 1.0).Sample();
        double systemNearZero = new ExponentialSampler(Create(UInt64MaximumOutputSeed, RandomMathMode.System), 1.0).Sample();

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(portableTail), Is.EqualTo(0x40425E4F7B2737FAUL));
            Assert.That(systemTail, Is.EqualTo(53.0 * Math.Log(2.0)).Within(1.0e-14));
            Assert.That(portableTail, Is.GreaterThan(36.0));
            Assert.That(double.IsFinite(portableTail), Is.True);
            Assert.That(double.IsFinite(systemTail), Is.True);
            Assert.That(PortableMath.DoubleBits(portableNearZero), Is.EqualTo(0x3CA0000000000000UL));
            Assert.That(PortableMath.DoubleBits(systemNearZero), Is.EqualTo(0x3CA0000000000000UL));
            Assert.That(portableNearZero, Is.GreaterThan(0.0));
            Assert.That(systemNearZero, Is.GreaterThan(0.0));
        });
    }

    [Test]
    public void ExtremeTailScalingCanOverflowToPositiveInfinity()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(OneOutputSeed, mode);
            RandomGenerator referenceGenerator = Create(OneOutputSeed, mode);
            double value = new ExponentialSampler(actualGenerator, double.MaxValue).Sample();
            referenceGenerator.NextDoubleOpenOpen();

            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(double.PositiveInfinity));
                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void PortableSequenceHasLockedBits()
    {
        ulong[] expected =
        [
            0x3FD497B9A6A6F25FUL,
            0x3FE6680FADF32200UL,
            0x3FE1B3B26FBFD4F5UL,
            0x3FBA3CC3F1F88915UL,
            0x3FBF0C42AA8A51FAUL,
            0x4000411A9C1D523EUL,
            0x3FF3987F90E2BD3AUL,
            0x3FD75ED49CB64B1FUL
        ];
        ExponentialSampler sampler = new ExponentialSampler(Create(42UL, RandomMathMode.Portable), 1.0);

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
