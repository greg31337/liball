namespace random.Tests;

public class NegativeBinomialSamplerTests
{
    [Test]
    public void UnitSuccessShapeMatchesFailureCountGeometricSampling()
    {
        RandomGenerator actualGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator referenceGenerator = Create(42UL, RandomMathMode.Portable);
        NegativeBinomialSampler actual = new NegativeBinomialSampler(actualGenerator, 1.0, 0.37);
        GeometricSampler reference = new GeometricSampler(referenceGenerator, 0.37);

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(actual.Sample(), Is.EqualTo(reference.SampleFailures()));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void SmallIntegerShapesMatchExactGeometricSums()
    {
        RandomGenerator actualGenerator = Create(123UL, RandomMathMode.Portable);
        RandomGenerator referenceGenerator = Create(123UL, RandomMathMode.Portable);
        byte[] before = actualGenerator.ExportState().ToBytes();
        NegativeBinomialSampler actual = new NegativeBinomialSampler(actualGenerator, 5.0, 0.25);
        GeometricSampler geometric = new GeometricSampler(referenceGenerator, 0.25);

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        for (int sampleIndex = 0; sampleIndex < 257; sampleIndex++)
        {
            long expected = 0L;
            for (int success = 0; success < 5; success++)
            {
                expected += geometric.SampleFailures();
            }

            Assert.That(actual.Sample(), Is.EqualTo(expected));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void FractionalAndLargeShapesMatchGammaPoissonComposition()
    {
        double[] shapes = [2.5, 40.0];
        foreach (double shape in shapes)
        {
            RandomGenerator actualGenerator = Create(234UL, RandomMathMode.Portable);
            RandomGenerator referenceGenerator = Create(234UL, RandomMathMode.Portable);
            NegativeBinomialSampler actual = new NegativeBinomialSampler(actualGenerator, shape, 0.37);
            GammaSampler gamma = new GammaSampler(referenceGenerator, shape, 1.0);
            double logOdds = PortableMath.Log1p(-0.37) - PortableMath.Log(0.37);

            for (int index = 0; index < 257; index++)
            {
                double gammaValue = gamma.Sample();
                double mean = PortableMath.Exp(PortableMath.Log(gammaValue) + logOdds);
                PoissonCountSampler poisson = new PoissonCountSampler(referenceGenerator, mean);
                Assert.That(actual.Sample(), Is.EqualTo(poisson.Sample()));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void FillMatchesRepeatedScalarSamplesAcrossBothPathsAndModes()
    {
        double[] shapes = [5.0, 40.0];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            foreach (double shape in shapes)
            {
                RandomGenerator fillGenerator = Create(345UL, mode);
                RandomGenerator scalarGenerator = Create(345UL, mode);
                NegativeBinomialSampler fillSampler = new NegativeBinomialSampler(fillGenerator, shape, 0.4);
                NegativeBinomialSampler scalarSampler = new NegativeBinomialSampler(scalarGenerator, shape, 0.4);
                long[] actual = new long[257];
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
                    Assert.That(actual, Is.All.GreaterThanOrEqualTo(0L));
                    Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
                });
            }
        }
    }

    [Test]
    public void CertainSuccessConsumesNoStateIncludingReservoirBits()
    {
        RandomGenerator generator = Create(456UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        NegativeBinomialSampler sampler = new NegativeBinomialSampler(generator, double.MaxValue, 1.0);
        long[] values = new long[32];

        long scalar = sampler.Sample();
        sampler.Fill(values);

        Assert.Multiple(() =>
        {
            Assert.That(scalar, Is.Zero);
            Assert.That(values, Is.All.Zero);
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void DirectAndGammaPoissonMomentsMatchTheFailureConvention()
    {
        AssertMoments(5.0, 0.4, 100000, 0.07, 0.5);
        AssertMoments(40.0, 0.4, 100000, 0.25, 4.0);
    }

    [Test]
    public void ExtremeParametersReportOutputCapacityExhaustion()
    {
        NegativeBinomialSampler direct = new NegativeBinomialSampler(
            Create(567UL, RandomMathMode.Portable),
            2.0,
            double.Epsilon);
        RandomGenerator gammaGenerator = Create(678UL, RandomMathMode.Portable);
        RandomGenerator gammaReferenceGenerator = Create(678UL, RandomMathMode.Portable);
        NegativeBinomialSampler mixture = new NegativeBinomialSampler(
            gammaGenerator,
            40.0,
            double.Epsilon);
        GammaSampler gammaReference = new GammaSampler(gammaReferenceGenerator, 40.0, 1.0);
        NegativeBinomialSampler hugeShape = new NegativeBinomialSampler(
            Create(789UL, RandomMathMode.Portable),
            double.MaxValue,
            0.5);

        Assert.Throws<RandomExhaustedException>(() => direct.Sample());
        Assert.Throws<RandomExhaustedException>(() => mixture.Sample());
        gammaReference.Sample();
        Assert.That(gammaGenerator.ExportState().ToBytes(), Is.EqualTo(gammaReferenceGenerator.ExportState().ToBytes()));
        Assert.Throws<RandomExhaustedException>(() => hugeShape.Sample());
    }

    [Test]
    public void InvalidArgumentsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullGenerator = Assert.Throws<RandomArgumentException>(() => new NegativeBinomialSampler(NullReference.Value<global::random.IRandomSource>(), 1.0, 0.5));
        Assert.That(nullGenerator.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(890UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new NegativeBinomialSampler(generator, 0.0, 0.5)), "successes" },
            new object[] { (TestDelegate)(() => new NegativeBinomialSampler(generator, -double.Epsilon, 0.5)), "successes" },
            new object[] { (TestDelegate)(() => new NegativeBinomialSampler(generator, double.NaN, 0.5)), "successes" },
            new object[] { (TestDelegate)(() => new NegativeBinomialSampler(generator, double.PositiveInfinity, 0.5)), "successes" },
            new object[] { (TestDelegate)(() => new NegativeBinomialSampler(generator, 1.0, 0.0)), "probability" },
            new object[] { (TestDelegate)(() => new NegativeBinomialSampler(generator, 1.0, -double.Epsilon)), "probability" },
            new object[] { (TestDelegate)(() => new NegativeBinomialSampler(generator, 1.0, PortableMath.NextUp(1.0))), "probability" },
            new object[] { (TestDelegate)(() => new NegativeBinomialSampler(generator, 1.0, double.NaN)), "probability" },
            new object[] { (TestDelegate)(() => new NegativeBinomialSampler(generator, 1.0, double.NegativeInfinity)), "probability" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static void AssertMoments(
        double successes,
        double probability,
        int sampleCount,
        double meanTolerance,
        double varianceTolerance)
    {
        NegativeBinomialSampler sampler = new NegativeBinomialSampler(
            Create(901UL, RandomMathMode.Portable),
            successes,
            probability);
        double sum = 0.0;
        double squareSum = 0.0;
        for (int index = 0; index < sampleCount; index++)
        {
            double value = sampler.Sample();
            sum += value;
            squareSum += value * value;
        }

        double actualMean = sum / sampleCount;
        double actualVariance = squareSum / sampleCount - actualMean * actualMean;
        double expectedMean = successes * (1.0 - probability) / probability;
        double expectedVariance = successes * (1.0 - probability) / (probability * probability);
        Assert.Multiple(() =>
        {
            Assert.That(actualMean, Is.EqualTo(expectedMean).Within(meanTolerance));
            Assert.That(actualVariance, Is.EqualTo(expectedVariance).Within(varianceTolerance));
        });
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
