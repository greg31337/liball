namespace random.Tests;

public class GammaSamplerTests
{
    [Test]
    public void UnitScaleConstructorMatchesExplicitScale()
    {
        RandomGenerator unitGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator explicitGenerator = Create(42UL, RandomMathMode.Portable);
        byte[] before = unitGenerator.ExportState().ToBytes();
        GammaSampler unit = new GammaSampler(unitGenerator, 2.75, 1.0);
        GammaSampler explicitSampler = new GammaSampler(explicitGenerator, 2.75, 1.0);

        Assert.That(unitGenerator.ExportState().ToBytes(), Is.EqualTo(before));

        for (int index = 0; index < 100; index++)
        {
            Assert.That(PortableMath.DoubleBits(unit.Sample()), Is.EqualTo(PortableMath.DoubleBits(explicitSampler.Sample())));
        }

        Assert.That(unitGenerator.ExportState().ToBytes(), Is.EqualTo(explicitGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void FillMatchesRepeatedSamplesAcrossShapeRegimesAndMathModes()
    {
        double[] shapes = [0.25, 1.0, 5.0];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (double shape in shapes)
        {
            foreach (RandomMathMode mode in modes)
            {
                RandomGenerator fillGenerator = Create(123456UL, mode);
                RandomGenerator scalarGenerator = Create(123456UL, mode);
                GammaSampler fillSampler = new GammaSampler(fillGenerator, shape, 3.5);
                GammaSampler scalarSampler = new GammaSampler(scalarGenerator, shape, 3.5);
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
    }

    [Test]
    public void ZeroScaleReturnsPositiveZeroWithoutConsumption()
    {
        double[] shapes = [double.Epsilon, 0.25, 1.0, 5.0, double.MaxValue];
        foreach (double shape in shapes)
        {
            RandomGenerator generator = Create(71UL, RandomMathMode.Portable);
            GammaSampler sampler = new GammaSampler(generator, shape, -0.0);
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
        RandomArgumentException nullUnit = Assert.Throws<RandomArgumentException>(() => new GammaSampler(NullReference.Value<global::random.IRandomSource>(), 1.0, 1.0));
        RandomArgumentException nullScaled = Assert.Throws<RandomArgumentException>(() => new GammaSampler(NullReference.Value<global::random.IRandomSource>(), 1.0, 1.0));
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
            new object[] { (TestDelegate)(() => new GammaSampler(generator, double.NaN, 1.0)), "shape" },
            new object[] { (TestDelegate)(() => new GammaSampler(generator, double.NegativeInfinity, 1.0)), "shape" },
            new object[] { (TestDelegate)(() => new GammaSampler(generator, double.PositiveInfinity, 1.0)), "shape" },
            new object[] { (TestDelegate)(() => new GammaSampler(generator, 0.0, 1.0)), "shape" },
            new object[] { (TestDelegate)(() => new GammaSampler(generator, -double.Epsilon, 1.0)), "shape" },
            new object[] { (TestDelegate)(() => new GammaSampler(generator, 1.0, double.NaN)), "scale" },
            new object[] { (TestDelegate)(() => new GammaSampler(generator, 1.0, double.PositiveInfinity)), "scale" },
            new object[] { (TestDelegate)(() => new GammaSampler(generator, 1.0, -double.Epsilon)), "scale" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void SmallShapeScalingAvoidsPrematureUnderflow()
    {
        RandomGenerator unitGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator scaledGenerator = Create(42UL, RandomMathMode.Portable);
        GammaSampler unit = new GammaSampler(unitGenerator, 0.0005, 1.0);
        GammaSampler scaled = new GammaSampler(scaledGenerator, 0.0005, 1.0e300);

        double underflowed = unit.Sample();
        double recovered = scaled.Sample();

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(underflowed), Is.EqualTo(0UL));
            Assert.That(double.IsFinite(recovered), Is.True);
            Assert.That(recovered, Is.GreaterThan(0.0));
            Assert.That(PortableMath.DoubleBits(recovered), Is.EqualTo(0x1A752B040FCCD60FUL));
            Assert.That(unitGenerator.ExportState().ToBytes(), Is.EqualTo(scaledGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void LargeShapesAvoidIntermediateOverflow()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator generator = Create(42UL, mode);
            RandomGenerator overflowGenerator = Create(42UL, mode);
            GammaSampler maximum = new GammaSampler(generator, double.MaxValue, 1.0);
            GammaSampler scaledMaximum = new GammaSampler(overflowGenerator, double.MaxValue, 2.0);
            byte[] before = generator.ExportState().ToBytes();

            for (int index = 0; index < 16; index++)
            {
                Assert.That(PortableMath.DoubleBits(maximum.Sample()), Is.EqualTo(PortableMath.DoubleBits(double.MaxValue)));
            }

            Assert.That(generator.ExportState().ToBytes(), Is.Not.EqualTo(before));
            Assert.That(scaledMaximum.Sample(), Is.EqualTo(double.PositiveInfinity));
        }
    }

    [Test]
    public void DeterministicMomentsCoverSmallUnitAndLargeShapeRegimes()
    {
        double[] shapes = [0.25, 1.0, 5.0, 100.0];
        double[] tolerances = [0.02, 0.04, 0.1, 0.5];
        const int sampleCount = 100000;

        for (int shapeIndex = 0; shapeIndex < shapes.Length; shapeIndex++)
        {
            double shape = shapes[shapeIndex];
            GammaSampler sampler = new GammaSampler(Create(123UL, RandomMathMode.Portable), shape, 2.0);
            double sum = 0.0;
            for (int index = 0; index < sampleCount; index++)
            {
                double value = sampler.Sample();
                Assert.That(value, Is.GreaterThanOrEqualTo(0.0));
                sum += value;
            }

            Assert.That(sum / sampleCount, Is.EqualTo(shape * 2.0).Within(tolerances[shapeIndex]));
        }
    }

    [Test]
    public void PortableSequencesLockBothShapeAlgorithms()
    {
        ulong[] smallExpected =
        [
            0x3FD1FDEB6ADC6752UL,
            0x3F43AA848622C13AUL,
            0x3F73C27781C96075UL,
            0x3FDDF22C8038EF6DUL
        ];
        ulong[] largeExpected =
        [
            0x4025C83A8F1EEA3EUL,
            0x402510EA8FC84B8BUL,
            0x40295C4BFE3C7C72UL,
            0x402D77ECBE8A235BUL
        ];
        GammaSampler small = new GammaSampler(Create(42UL, RandomMathMode.Portable), 0.25, 2.0);
        GammaSampler large = new GammaSampler(Create(42UL, RandomMathMode.Portable), 5.0, 2.0);

        foreach (ulong expectedBits in smallExpected)
        {
            Assert.That(PortableMath.DoubleBits(small.Sample()), Is.EqualTo(expectedBits));
        }

        foreach (ulong expectedBits in largeExpected)
        {
            Assert.That(PortableMath.DoubleBits(large.Sample()), Is.EqualTo(expectedBits));
        }
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
