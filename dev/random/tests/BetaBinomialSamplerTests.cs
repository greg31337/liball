namespace random.Tests;

public class BetaBinomialSamplerTests
{
    [Test]
    public void SamplesMatchExplicitBetaThenBinomialComposition()
    {
        double[][] cases = [[17.0, 0.25, 0.75], [1000.0, 2.0, 5.0], [10000.0, 0.5, 0.5]];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            foreach (double[] testCase in cases)
            {
                int trials = (int)testCase[0];
                RandomGenerator actualGenerator = Create(42UL, mode);
                RandomGenerator referenceGenerator = Create(42UL, mode);
                byte[] before = actualGenerator.ExportState().ToBytes();
                BetaBinomialSampler actual = new BetaBinomialSampler(
                    actualGenerator,
                    trials,
                    testCase[1],
                    testCase[2]);
                BetaSampler beta = new BetaSampler(referenceGenerator, testCase[1], testCase[2]);

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
                for (int index = 0; index < 257; index++)
                {
                    double probability = beta.Sample();
                    BinomialSampler conditional = new BinomialSampler(referenceGenerator, trials, probability);
                    Assert.That(actual.Sample(), Is.EqualTo(conditional.Sample()));
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void FillMatchesRepeatedScalarSamplesExactly()
    {
        RandomGenerator fillGenerator = Create(123UL, RandomMathMode.Portable);
        RandomGenerator scalarGenerator = Create(123UL, RandomMathMode.Portable);
        BetaBinomialSampler fillSampler = new BetaBinomialSampler(fillGenerator, 1000, 2.0, 5.0);
        BetaBinomialSampler scalarSampler = new BetaBinomialSampler(scalarGenerator, 1000, 2.0, 5.0);
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
            Assert.That(actual, Is.All.InRange(0, 1000));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ZeroTrialsConsumeNoStateIncludingReservoirBits()
    {
        RandomGenerator generator = Create(234UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        BetaBinomialSampler sampler = new BetaBinomialSampler(generator, 0, 0.25, 0.75);
        int[] values = new int[32];

        int scalar = sampler.Sample();
        sampler.Fill(values);

        Assert.Multiple(() =>
        {
            Assert.That(scalar, Is.Zero);
            Assert.That(values, Is.All.Zero);
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void LargestTrialCountDoesNotOverflow()
    {
        BetaBinomialSampler balanced = new BetaBinomialSampler(
            Create(345UL, RandomMathMode.Portable),
            int.MaxValue,
            double.MaxValue,
            double.MaxValue);
        BetaBinomialSampler upper = new BetaBinomialSampler(
            Create(456UL, RandomMathMode.Portable),
            int.MaxValue,
            double.MaxValue,
            double.Epsilon);
        BetaBinomialSampler lower = new BetaBinomialSampler(
            Create(567UL, RandomMathMode.Portable),
            int.MaxValue,
            double.Epsilon,
            double.MaxValue);

        for (int index = 0; index < 128; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(balanced.Sample(), Is.InRange(0, int.MaxValue));
                Assert.That(upper.Sample(), Is.EqualTo(int.MaxValue));
                Assert.That(lower.Sample(), Is.Zero);
            });
        }
    }

    [Test]
    public void DeterministicMomentsMatchBetaBinomialFormulas()
    {
        const int trials = 50;
        const double alpha = 2.0;
        const double beta = 5.0;
        const int sampleCount = 50000;
        BetaBinomialSampler sampler = new BetaBinomialSampler(
            Create(678UL, RandomMathMode.Portable),
            trials,
            alpha,
            beta);
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
        double shapeSum = alpha + beta;
        double expectedMean = (double)trials * alpha / shapeSum;
        double expectedVariance = (double)trials * alpha * beta * (shapeSum + (double)trials);
        expectedVariance /= shapeSum * shapeSum * (shapeSum + 1.0);
        Assert.Multiple(() =>
        {
            Assert.That(actualMean, Is.EqualTo(expectedMean).Within(0.15));
            Assert.That(actualVariance, Is.EqualTo(expectedVariance).Within(2.5));
        });
    }

    [Test]
    public void InvalidArgumentsAreRejectedWithoutConsumption()
    {
        RandomArgumentException nullGenerator = Assert.Throws<RandomArgumentException>(() => new BetaBinomialSampler(NullReference.Value<global::random.IRandomSource>(), 1, 1.0, 1.0));
        Assert.That(nullGenerator.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(789UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new BetaBinomialSampler(generator, -1, 1.0, 1.0)), "trials" },
            new object[] { (TestDelegate)(() => new BetaBinomialSampler(generator, 1, 0.0, 1.0)), "alpha" },
            new object[] { (TestDelegate)(() => new BetaBinomialSampler(generator, 1, -double.Epsilon, 1.0)), "alpha" },
            new object[] { (TestDelegate)(() => new BetaBinomialSampler(generator, 1, double.NaN, 1.0)), "alpha" },
            new object[] { (TestDelegate)(() => new BetaBinomialSampler(generator, 1, double.PositiveInfinity, 1.0)), "alpha" },
            new object[] { (TestDelegate)(() => new BetaBinomialSampler(generator, 1, 1.0, 0.0)), "beta" },
            new object[] { (TestDelegate)(() => new BetaBinomialSampler(generator, 1, 1.0, -double.Epsilon)), "beta" },
            new object[] { (TestDelegate)(() => new BetaBinomialSampler(generator, 1, 1.0, double.NaN)), "beta" },
            new object[] { (TestDelegate)(() => new BetaBinomialSampler(generator, 1, 1.0, double.NegativeInfinity)), "beta" }
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
