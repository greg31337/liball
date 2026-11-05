namespace random.Tests;

public class MultivariateHypergeometricSamplerTests
{
    [Test]
    public void SamplesMatchSequentialConditionalHypergeometricComposition()
    {
        int[] counts = [20, 0, 30, 50];
        RandomGenerator actualGenerator = Create(42UL);
        RandomGenerator referenceGenerator = Create(42UL);
        byte[] before = actualGenerator.ExportState().ToBytes();
        MultivariateHypergeometricSampler sampler = new MultivariateHypergeometricSampler(actualGenerator, counts, 40);

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        for (int sampleIndex = 0; sampleIndex < 257; sampleIndex++)
        {
            int[] actual = sampler.Sample();
            int[] expected = SampleConditional(referenceGenerator, counts, 40);
            Assert.That(actual, Is.EqualTo(expected));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void EveryVectorPreservesRequestedTotalAndCategoryBounds()
    {
        int[] counts = [7, 13, 0, 29, 51];
        MultivariateHypergeometricSampler sampler = new MultivariateHypergeometricSampler(Create(123UL), counts, 37);

        for (int sampleIndex = 0; sampleIndex < 10000; sampleIndex++)
        {
            int[] values = sampler.Sample();
            int total = 0;
            for (int index = 0; index < values.Length; index++)
            {
                Assert.That(values[index], Is.InRange(0, counts[index]));
                total += values[index];
            }

            Assert.That(total, Is.EqualTo(37));
        }
    }

    [Test]
    public void CategoryMeansMatchSamplingWithoutReplacement()
    {
        int[] counts = [20, 30, 50];
        const int draws = 40;
        const int sampleCount = 100000;
        MultivariateHypergeometricSampler sampler = new MultivariateHypergeometricSampler(Create(234UL), counts, draws);
        double[] sums = new double[counts.Length];

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
            Assert.That(sums[0] / sampleCount, Is.EqualTo(8.0).Within(0.04));
            Assert.That(sums[1] / sampleCount, Is.EqualTo(12.0).Within(0.04));
            Assert.That(sums[2] / sampleCount, Is.EqualTo(20.0).Within(0.04));
        });
    }

    [Test]
    public void AllocatingAndDestinationSamplesMatchExactly()
    {
        RandomGenerator allocatingGenerator = Create(345UL);
        RandomGenerator destinationGenerator = Create(345UL);
        MultivariateHypergeometricSampler allocating = new MultivariateHypergeometricSampler(
            allocatingGenerator,
            [100, 200, 300, 400],
            333);
        MultivariateHypergeometricSampler destination = new MultivariateHypergeometricSampler(
            destinationGenerator,
            [100, 200, 300, 400],
            333);
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
    public void ConstructorDefensivelyCopiesCategoryCounts()
    {
        int[] counts = [20, 30, 50];
        RandomGenerator actualGenerator = Create(456UL);
        RandomGenerator referenceGenerator = Create(456UL);
        MultivariateHypergeometricSampler actual = new MultivariateHypergeometricSampler(actualGenerator, counts, 40);
        MultivariateHypergeometricSampler reference = new MultivariateHypergeometricSampler(referenceGenerator, [20, 30, 50], 40);

        counts[0] = -1;
        counts[1] = int.MaxValue;
        counts[2] = 0;
        for (int index = 0; index < 257; index++)
        {
            Assert.That(actual.Sample(), Is.EqualTo(reference.Sample()));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void DegenerateVectorsConsumeNoStateIncludingReservoirBits()
    {
        RandomGenerator generator = Create(567UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        MultivariateHypergeometricSampler noDraws = new MultivariateHypergeometricSampler(generator, [10, 20, 30], 0);
        MultivariateHypergeometricSampler allDraws = new MultivariateHypergeometricSampler(generator, [10, 20, 30], 60);
        MultivariateHypergeometricSampler onePositive = new MultivariateHypergeometricSampler(generator, [0, 50, 0], 17);
        MultivariateHypergeometricSampler oneCategory = new MultivariateHypergeometricSampler(generator, [100], 42);

        Assert.Multiple(() =>
        {
            Assert.That(noDraws.Sample(), Is.EqualTo(new int[] { 0, 0, 0 }));
            Assert.That(allDraws.Sample(), Is.EqualTo(new int[] { 10, 20, 30 }));
            Assert.That(onePositive.Sample(), Is.EqualTo(new int[] { 0, 17, 0 }));
            Assert.That(oneCategory.Sample(), Is.EqualTo(new int[] { 42 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void MaximumPopulationTotalRemainsExact()
    {
        int[] counts = [int.MaxValue - 10, 10];
        MultivariateHypergeometricSampler sampler = new MultivariateHypergeometricSampler(
            Create(678UL),
            counts,
            1073741824);

        for (int index = 0; index < 257; index++)
        {
            int[] values = sampler.Sample();
            Assert.Multiple(() =>
            {
                Assert.That(values[0] + values[1], Is.EqualTo(1073741824));
                Assert.That(values[0], Is.InRange(0, counts[0]));
                Assert.That(values[1], Is.InRange(0, counts[1]));
            });
        }
    }

    [Test]
    public void InvalidArgumentsAndDestinationsAreRejectedBeforeMutation()
    {
        Assert.That(
            Assert.Throws<RandomArgumentException>(() => new MultivariateHypergeometricSampler(NullReference.Value<global::random.IRandomSource>(), [1], 1))?.ArgumentName(),
            Is.EqualTo("generator"));

        RandomGenerator generator = Create(789UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new MultivariateHypergeometricSampler(generator, NullReference.Value<int[]>(), 0)), "categoryCounts" },
            new object[] { (TestDelegate)(() => new MultivariateHypergeometricSampler(generator, [], 0)), "categoryCounts" },
            new object[] { (TestDelegate)(() => new MultivariateHypergeometricSampler(generator, [1, -1], 0)), "categoryCounts" },
            new object[] { (TestDelegate)(() => new MultivariateHypergeometricSampler(generator, [int.MaxValue, 1], 0)), "categoryCounts" },
            new object[] { (TestDelegate)(() => new MultivariateHypergeometricSampler(generator, [1], -1)), "draws" },
            new object[] { (TestDelegate)(() => new MultivariateHypergeometricSampler(generator, [1], 2)), "draws" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }

        MultivariateHypergeometricSampler sampler = new MultivariateHypergeometricSampler(generator, [10, 20, 30], 15);
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

    static int[] SampleConditional(RandomGenerator generator, int[] counts, int draws)
    {
        int total = 0;
        foreach (int count in counts)
        {
            total += count;
        }

        int[] result = new int[counts.Length];
        int remainingPopulation = total;
        int remainingDraws = draws;
        for (int index = 0; index < counts.Length - 1; index++)
        {
            HypergeometricSampler conditional = new HypergeometricSampler(
                generator,
                remainingPopulation,
                counts[index],
                remainingDraws);
            result[index] = conditional.Sample();
            remainingPopulation -= counts[index];
            remainingDraws -= result[index];
        }

        result[counts.Length - 1] = remainingDraws;
        return result;
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
