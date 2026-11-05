namespace random.Tests;

public class MultinomialSamplerTests
{
    [Test]
    public void SamplesMatchSequentialConditionalBinomialComposition()
    {
        RandomGenerator actualGenerator = Create(42UL);
        RandomGenerator referenceGenerator = Create(42UL);
        byte[] before = actualGenerator.ExportState().ToBytes();
        MultinomialSampler sampler = new MultinomialSampler(actualGenerator, 1000, [1.0, 1.0, 0.0, 2.0]);

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        for (int sampleIndex = 0; sampleIndex < 257; sampleIndex++)
        {
            int[] expected = new int[4];
            expected[0] = new BinomialSampler(referenceGenerator, 1000, 0.25).Sample();
            int remaining = 1000 - expected[0];
            expected[1] = new BinomialSampler(referenceGenerator, remaining, 1.0 / 3.0).Sample();
            expected[2] = 0;
            expected[3] = remaining - expected[1];

            Assert.That(sampler.Sample(), Is.EqualTo(expected));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void EveryVectorPreservesTheTrialTotalAndZeroProbabilities()
    {
        MultinomialSampler sampler = new MultinomialSampler(Create(123UL), 337, [0.0, 1.0, 2.0, 0.0, 7.0, 0.0]);

        for (int sampleIndex = 0; sampleIndex < 10000; sampleIndex++)
        {
            int[] values = sampler.Sample();
            int total = 0;
            foreach (int value in values)
            {
                Assert.That(value, Is.GreaterThanOrEqualTo(0));
                total += value;
            }

            Assert.Multiple(() =>
            {
                Assert.That(total, Is.EqualTo(337));
                Assert.That(values[0], Is.Zero);
                Assert.That(values[3], Is.Zero);
                Assert.That(values[5], Is.Zero);
            });
        }
    }

    [Test]
    public void RelativeProbabilityScalingProducesTheSameSequence()
    {
        RandomGenerator baseGenerator = Create(234UL);
        RandomGenerator scaledGenerator = Create(234UL);
        MultinomialSampler baseSampler = new MultinomialSampler(baseGenerator, 1000, [1.0, 2.0, 0.0, 7.0]);
        MultinomialSampler scaledSampler = new MultinomialSampler(scaledGenerator, 1000, [16.0, 32.0, 0.0, 112.0]);

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(baseSampler.Sample(), Is.EqualTo(scaledSampler.Sample()));
        }

        Assert.That(baseGenerator.ExportState().ToBytes(), Is.EqualTo(scaledGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void CategoryMeansMatchNormalizedProbabilities()
    {
        const int sampleCount = 100000;
        MultinomialSampler sampler = new MultinomialSampler(Create(345UL), 100, [1.0, 2.0, 7.0]);
        double[] sums = new double[3];
        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            int[] values = sampler.Sample();
            for (int index = 0; index < values.Length; index++)
            {
                sums[index] += values[index];
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(sums[0] / sampleCount, Is.EqualTo(10.0).Within(0.04));
            Assert.That(sums[1] / sampleCount, Is.EqualTo(20.0).Within(0.05));
            Assert.That(sums[2] / sampleCount, Is.EqualTo(70.0).Within(0.06));
        });
    }

    [Test]
    public void AllocatingAndDestinationSamplesMatchExactly()
    {
        RandomGenerator allocatingGenerator = Create(456UL);
        RandomGenerator destinationGenerator = Create(456UL);
        MultinomialSampler allocating = new MultinomialSampler(allocatingGenerator, 1000, [1.0, 3.0, 0.0, 6.0]);
        MultinomialSampler destination = new MultinomialSampler(destinationGenerator, 1000, [1.0, 3.0, 0.0, 6.0]);
        int[] actual = new int[4];

        for (int index = 0; index < 257; index++)
        {
            int[] expected = allocating.Sample();
            destination.SampleInto(actual);
            Assert.That(actual, Is.EqualTo(expected));
        }

        Assert.That(destinationGenerator.ExportState().ToBytes(), Is.EqualTo(allocatingGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void ConstructorDefensivelyPreprocessesProbabilities()
    {
        double[] probabilities = [1.0, 3.0, 0.0, 6.0];
        RandomGenerator actualGenerator = Create(567UL);
        RandomGenerator referenceGenerator = Create(567UL);
        MultinomialSampler actual = new MultinomialSampler(actualGenerator, 1000, probabilities);
        MultinomialSampler reference = new MultinomialSampler(referenceGenerator, 1000, [1.0, 3.0, 0.0, 6.0]);

        probabilities[0] = double.NaN;
        probabilities[1] = 0.0;
        probabilities[2] = double.PositiveInfinity;
        probabilities[3] = 0.0;
        for (int index = 0; index < 257; index++)
        {
            Assert.That(actual.Sample(), Is.EqualTo(reference.Sample()));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void DegenerateAndMaximumTrialVectorsRemainExact()
    {
        RandomGenerator generator = Create(678UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        MultinomialSampler noTrials = new MultinomialSampler(generator, 0, [1.0, 2.0, 7.0]);
        MultinomialSampler onePositive = new MultinomialSampler(generator, int.MaxValue, [0.0, 5.0, 0.0]);

        Assert.Multiple(() =>
        {
            Assert.That(noTrials.Sample(), Is.EqualTo(new int[] { 0, 0, 0 }));
            Assert.That(onePositive.Sample(), Is.EqualTo(new int[] { 0, int.MaxValue, 0 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });

        MultinomialSampler balanced = new MultinomialSampler(Create(789UL), int.MaxValue, [1.0, 1.0]);
        for (int index = 0; index < 257; index++)
        {
            int[] values = balanced.Sample();
            Assert.That((long)values[0] + (long)values[1], Is.EqualTo((long)int.MaxValue));
        }
    }

    [Test]
    public void InvalidArgumentsAndDestinationsAreRejectedBeforeMutation()
    {
        Assert.That(
            Assert.Throws<RandomArgumentException>(() => new MultinomialSampler(NullReference.Value<global::random.IRandomSource>(), 1, [1.0]))?.ArgumentName(),
            Is.EqualTo("generator"));

        RandomGenerator generator = Create(890UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new MultinomialSampler(generator, -1, [1.0])), "trials" },
            new object[] { (TestDelegate)(() => new MultinomialSampler(generator, 1, NullReference.Value<double[]>())), "probabilities" },
            new object[] { (TestDelegate)(() => new MultinomialSampler(generator, 1, [])), "probabilities" },
            new object[] { (TestDelegate)(() => new MultinomialSampler(generator, 1, [0.0, 0.0])), "probabilities" },
            new object[] { (TestDelegate)(() => new MultinomialSampler(generator, 1, [1.0, -double.Epsilon])), "probabilities" },
            new object[] { (TestDelegate)(() => new MultinomialSampler(generator, 1, [1.0, double.NaN])), "probabilities" },
            new object[] { (TestDelegate)(() => new MultinomialSampler(generator, 1, [1.0, double.PositiveInfinity])), "probabilities" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }

        MultinomialSampler sampler = new MultinomialSampler(generator, 100, [1.0, 2.0, 7.0]);
        int[] shortDestination = [91, 92];
        int[] longDestination = [91, 92, 93, 94];
        Assert.That(Assert.Throws<RandomArgumentException>(() => sampler.SampleInto(shortDestination))?.ArgumentName(), Is.EqualTo("destination"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => sampler.SampleInto(longDestination))?.ArgumentName(), Is.EqualTo("destination"));
        Assert.Multiple(() =>
        {
            Assert.That(shortDestination, Is.EqualTo(new int[] { 91, 92 }));
            Assert.That(longDestination, Is.EqualTo(new int[] { 91, 92, 93, 94 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            Assert.That(sampler.CategoryCount(), Is.EqualTo(3));
        });
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
