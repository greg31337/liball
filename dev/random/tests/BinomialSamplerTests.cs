namespace random.Tests;

public class BinomialSamplerTests
{
    [Test]
    public void SmallTrialPathMatchesExactBernoulliSummation()
    {
        RandomGenerator actualGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator referenceGenerator = Create(42UL, RandomMathMode.Portable);
        byte[] before = actualGenerator.ExportState().ToBytes();
        BinomialSampler sampler = new BinomialSampler(actualGenerator, 17, 0.37);

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        for (int sampleIndex = 0; sampleIndex < 128; sampleIndex++)
        {
            int expected = 0;
            for (int trial = 0; trial < 17; trial++)
            {
                if (referenceGenerator.NextBooleanWithDoubleProbability(0.37))
                {
                    expected++;
                }
            }

            Assert.That(sampler.Sample(), Is.EqualTo(expected));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void ProbabilitySymmetryUsesTheSameReducedVariates()
    {
        RandomGenerator lowGenerator = Create(123UL, RandomMathMode.Portable);
        RandomGenerator highGenerator = Create(123UL, RandomMathMode.Portable);
        BinomialSampler low = new BinomialSampler(lowGenerator, 1000, 0.25);
        BinomialSampler high = new BinomialSampler(highGenerator, 1000, 0.75);

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(high.Sample(), Is.EqualTo(1000 - low.Sample()));
        }

        Assert.That(highGenerator.ExportState().ToBytes(), Is.EqualTo(lowGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void FillMatchesRepeatedScalarSamplesAcrossEveryRegime()
    {
        int[][] cases = [[31, 370000], [10000, 1000], [1000, 370000]];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            foreach (int[] testCase in cases)
            {
                int trials = testCase[0];
                double probability = (double)testCase[1] / 1000000.0;
                RandomGenerator fillGenerator = Create(234UL, mode);
                RandomGenerator scalarGenerator = Create(234UL, mode);
                BinomialSampler fillSampler = new BinomialSampler(fillGenerator, trials, probability);
                BinomialSampler scalarSampler = new BinomialSampler(scalarGenerator, trials, probability);
                int[] actual = new int[257];
                int[] expected = new int[actual.Length];
                byte[] beforeEmpty = fillGenerator.ExportState().ToBytes();

                fillSampler.Fill(Span<int>.Empty);
                Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(beforeEmpty));
                fillSampler.Fill(actual);
                for (int index = 0; index < expected.Length; index++)
                {
                    expected[index] = scalarSampler.Sample();
                }

                Assert.Multiple(() =>
                {
                    Assert.That(actual, Is.EqualTo(expected));
                    Assert.That(actual, Is.All.InRange(0, trials));
                    Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
                });
            }
        }
    }

    [Test]
    public void DeterministicCasesConsumeNoStateIncludingReservoirBits()
    {
        RandomGenerator generator = Create(345UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        BinomialSampler noTrials = new BinomialSampler(generator, 0, 0.4);
        BinomialSampler impossible = new BinomialSampler(generator, 1000, 0.0);
        BinomialSampler certain = new BinomialSampler(generator, 1000, 1.0);
        int[] zeroValues = new int[16];
        int[] certainValues = new int[16];

        int noTrialValue = noTrials.Sample();
        impossible.Fill(zeroValues);
        certain.Fill(certainValues);

        Assert.Multiple(() =>
        {
            Assert.That(noTrialValue, Is.Zero);
            Assert.That(zeroValues, Is.All.Zero);
            Assert.That(certainValues, Is.All.EqualTo(1000));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InversionAndTransformedRejectionHaveExpectedMoments()
    {
        AssertMoments(10000, 0.001, 100000, 0.06, 0.35);
        AssertMoments(1000, 0.37, 100000, 0.25, 4.0);
    }

    [Test]
    public void LargestTrialCountRemainsInsideSupport()
    {
        BinomialSampler central = new BinomialSampler(Create(456UL, RandomMathMode.Portable), int.MaxValue, 0.5);
        BinomialSampler rare = new BinomialSampler(Create(567UL, RandomMathMode.Portable), int.MaxValue, 1.0e-9);

        for (int index = 0; index < 1000; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(central.Sample(), Is.InRange(0, int.MaxValue));
                Assert.That(rare.Sample(), Is.InRange(0, int.MaxValue));
            });
        }
    }

    [Test]
    public void InvalidArgumentsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullGenerator = Assert.Throws<RandomArgumentException>(() => new BinomialSampler(NullReference.Value<global::random.IRandomSource>(), 1, 0.5));
        Assert.That(nullGenerator.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(678UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new BinomialSampler(generator, -1, 0.5)), "trials" },
            new object[] { (TestDelegate)(() => new BinomialSampler(generator, 1, -double.Epsilon)), "probability" },
            new object[] { (TestDelegate)(() => new BinomialSampler(generator, 1, PortableMath.NextUp(1.0))), "probability" },
            new object[] { (TestDelegate)(() => new BinomialSampler(generator, 1, double.NaN)), "probability" },
            new object[] { (TestDelegate)(() => new BinomialSampler(generator, 1, double.NegativeInfinity)), "probability" },
            new object[] { (TestDelegate)(() => new BinomialSampler(generator, 1, double.PositiveInfinity)), "probability" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static void AssertMoments(
        int trials,
        double probability,
        int sampleCount,
        double meanTolerance,
        double varianceTolerance)
    {
        BinomialSampler sampler = new BinomialSampler(Create(789UL, RandomMathMode.Portable), trials, probability);
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
        double expectedMean = (double)trials * probability;
        double expectedVariance = expectedMean * (1.0 - probability);
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
