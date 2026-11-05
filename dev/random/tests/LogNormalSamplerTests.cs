namespace random.Tests;

public class LogNormalSamplerTests
{
    [Test]
    public void StandardConstructorMatchesExplicitUnderlyingParameters()
    {
        RandomGenerator standardGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator explicitGenerator = Create(42UL, RandomMathMode.Portable);
        byte[] before = standardGenerator.ExportState().ToBytes();
        LogNormalSampler standard = new LogNormalSampler(standardGenerator, 0.0, 1.0);
        LogNormalSampler explicitSampler = new LogNormalSampler(explicitGenerator, 0.0, 1.0);

        Assert.That(standardGenerator.ExportState().ToBytes(), Is.EqualTo(before));

        for (int index = 0; index < 100; index++)
        {
            Assert.That(PortableMath.DoubleBits(standard.Sample()), Is.EqualTo(PortableMath.DoubleBits(explicitSampler.Sample())));
        }

        Assert.That(standardGenerator.ExportState().ToBytes(), Is.EqualTo(explicitGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void SamplesAreExactTransformsOfNormalSamplesInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(987654UL, mode);
            RandomGenerator referenceGenerator = Create(987654UL, mode);
            LogNormalSampler actual = new LogNormalSampler(actualGenerator, -0.75, 2.25);
            NormalSampler normal = new NormalSampler(referenceGenerator, -0.75, 2.25);

            for (int index = 0; index < 500; index++)
            {
                double exponent = normal.Sample();
                double expected = mode == RandomMathMode.Portable ? PortableMath.Exp(exponent) : Math.Exp(exponent);
                Assert.That(PortableMath.DoubleBits(actual.Sample()), Is.EqualTo(PortableMath.DoubleBits(expected)));
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
            LogNormalSampler fillSampler = new LogNormalSampler(fillGenerator, 1.25, 0.75);
            LogNormalSampler scalarSampler = new LogNormalSampler(scalarGenerator, 1.25, 0.75);
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
    public void ZeroLogDeviationReturnsPointMassWithoutConsumption()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator generator = Create(71UL, mode);
            LogNormalSampler sampler = new LogNormalSampler(generator, 1.0, -0.0);
            byte[] before = generator.ExportState().ToBytes();
            double expected = mode == RandomMathMode.Portable ? PortableMath.Exp(1.0) : Math.Exp(1.0);
            double[] values = new double[16];

            double scalar = sampler.Sample();
            sampler.Fill(values);

            Assert.Multiple(() =>
            {
                Assert.That(PortableMath.DoubleBits(scalar), Is.EqualTo(PortableMath.DoubleBits(expected)));
                Assert.That(values, Is.All.EqualTo(expected));
                Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            });
        }
    }

    [Test]
    public void OverflowAndUnderflowHaveDocumentedResultsWithoutConsumption()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator generator = Create(19UL, mode);
            byte[] before = generator.ExportState().ToBytes();
            double overflow = new LogNormalSampler(generator, double.MaxValue, 0.0).Sample();
            double underflow = new LogNormalSampler(generator, -double.MaxValue, 0.0).Sample();
            double highFinite = new LogNormalSampler(generator, 709.0, 0.0).Sample();
            double minimumSubnormal = new LogNormalSampler(generator, -745.0, 0.0).Sample();

            Assert.Multiple(() =>
            {
                Assert.That(overflow, Is.EqualTo(double.PositiveInfinity));
                Assert.That(PortableMath.DoubleBits(underflow), Is.EqualTo(0UL));
                Assert.That(double.IsFinite(highFinite), Is.True);
                Assert.That(highFinite, Is.GreaterThan(0.0));
                Assert.That(PortableMath.DoubleBits(minimumSubnormal), Is.EqualTo(1UL));
                Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            });
        }
    }

    [Test]
    public void InfiniteNormalTransformsMapToLogNormalLimits()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator positiveGenerator = Create(42UL, mode);
            RandomGenerator positiveReference = Create(42UL, mode);
            double positiveExponent = new NormalSampler(positiveReference, double.MaxValue, double.MaxValue).Sample();
            double overflow = new LogNormalSampler(positiveGenerator, double.MaxValue, double.MaxValue).Sample();

            RandomGenerator negativeGenerator = Create(42UL, mode);
            RandomGenerator negativeReference = Create(42UL, mode);
            new NormalSampler(negativeGenerator, 0.0, 1.0).Sample();
            new NormalSampler(negativeReference, 0.0, 1.0).Sample();
            double negativeExponent = new NormalSampler(negativeReference, -double.MaxValue, double.MaxValue).Sample();
            double underflow = new LogNormalSampler(negativeGenerator, -double.MaxValue, double.MaxValue).Sample();

            Assert.Multiple(() =>
            {
                Assert.That(positiveExponent, Is.EqualTo(double.PositiveInfinity));
                Assert.That(overflow, Is.EqualTo(double.PositiveInfinity));
                Assert.That(negativeExponent, Is.EqualTo(double.NegativeInfinity));
                Assert.That(PortableMath.DoubleBits(underflow), Is.EqualTo(0UL));
                Assert.That(positiveGenerator.ExportState().ToBytes(), Is.EqualTo(positiveReference.ExportState().ToBytes()));
                Assert.That(negativeGenerator.ExportState().ToBytes(), Is.EqualTo(negativeReference.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void InvalidConstructorArgumentsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullStandard = Assert.Throws<RandomArgumentException>(() => new LogNormalSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0));
        RandomArgumentException nullScaled = Assert.Throws<RandomArgumentException>(() => new LogNormalSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0));
        Assert.Multiple(() =>
        {
            Assert.That(nullStandard.ArgumentName(), Is.EqualTo("generator"));
            Assert.That(nullScaled.ArgumentName(), Is.EqualTo("generator"));
        });

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new LogNormalSampler(generator, double.NaN, 1.0)), "logMean" },
            new object[] { (TestDelegate)(() => new LogNormalSampler(generator, double.NegativeInfinity, 1.0)), "logMean" },
            new object[] { (TestDelegate)(() => new LogNormalSampler(generator, 0.0, double.NaN)), "logStandardDeviation" },
            new object[] { (TestDelegate)(() => new LogNormalSampler(generator, 0.0, double.PositiveInfinity)), "logStandardDeviation" },
            new object[] { (TestDelegate)(() => new LogNormalSampler(generator, 0.0, -double.Epsilon)), "logStandardDeviation" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void PortableSequenceHasLockedBits()
    {
        ulong[] expected =
        [
            0x3FF68720B6C3831FUL,
            0x3FE8B561BF072799UL,
            0x3FF4E33509E5125FUL,
            0x3FF1AEF4140C54E6UL,
            0x40000FDCF0D6E0E8UL,
            0x3FF4D5F6FC9024E4UL,
            0x400736F21EDFECEAUL,
            0x3FDAA96124402EE5UL
        ];
        LogNormalSampler sampler = new LogNormalSampler(Create(42UL, RandomMathMode.Portable), 0.0, 1.0);

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
