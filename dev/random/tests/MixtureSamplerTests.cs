namespace random.Tests;

public class MixtureSamplerTests
{
    [Test]
    public void SelectionMatchesAliasTableAndInvokesOnlySelectedComponents()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator expectedGenerator = Create(101UL);
        int[] invocationCounts = new int[4];
        Func<double>[] components =
        [
            () => ReturnAndCount(10.0, invocationCounts, 0),
            () => ReturnAndCount(20.0, invocationCounts, 1),
            () => ReturnAndCount(30.0, invocationCounts, 2),
            () => ReturnAndCount(40.0, invocationCounts, 3)
        ];
        double[] weights = [0.0, 1.0, 3.0, 6.0];
        MixtureSampler sampler = new MixtureSampler(actualGenerator, components, weights);
        AliasTable expectedTable = new AliasTable(weights);
        int[] expectedCounts = new int[4];

        for (int index = 0; index < 5000; index++)
        {
            int selected = expectedTable.Sample(expectedGenerator);
            expectedCounts[selected]++;
            Assert.That(sampler.Sample(), Is.EqualTo((double)(selected + 1) * 10.0));
        }

        Assert.Multiple(() =>
        {
            Assert.That(sampler.ComponentCount(), Is.EqualTo(4));
            Assert.That(invocationCounts, Is.EqualTo(expectedCounts));
            Assert.That(invocationCounts[0], Is.EqualTo(0));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ComponentSelectionPrecedesInvocationOnASharedGenerator()
    {
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator expectedGenerator = Create(202UL);
        Func<double>[] components =
        [
            () => actualGenerator.NextDouble(),
            () => 10.0 + actualGenerator.NextDouble(),
            () => 20.0 + actualGenerator.NextDouble()
        ];
        double[] weights = [1.0, 2.0, 3.0];
        MixtureSampler sampler = new MixtureSampler(actualGenerator, components, weights);
        AliasTable expectedTable = new AliasTable(weights);

        for (int index = 0; index < 2000; index++)
        {
            int selected = expectedTable.Sample(expectedGenerator);
            double expected = (double)selected * 10.0 + expectedGenerator.NextDouble();
            Assert.That(sampler.Sample(), Is.EqualTo(expected));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void WeightedFrequenciesMatchMixtureMasses()
    {
        const int sampleCount = 100000;
        MixtureSampler sampler = new MixtureSampler(
            Create(303UL),
            [() => 0.0, () => 1.0, () => 2.0, () => 3.0],
            [0.0, 1.0, 3.0, 6.0]);
        int[] counts = new int[4];
        for (int index = 0; index < sampleCount; index++)
        {
            counts[(int)sampler.Sample()]++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(counts[0], Is.EqualTo(0));
            Assert.That((double)counts[1] / sampleCount, Is.EqualTo(0.1).Within(0.006));
            Assert.That((double)counts[2] / sampleCount, Is.EqualTo(0.3).Within(0.008));
            Assert.That((double)counts[3] / sampleCount, Is.EqualTo(0.6).Within(0.008));
        });
    }

    [Test]
    public void FillMatchesRepeatedSamplesAndComponentEffects()
    {
        RandomGenerator fillGenerator = Create(404UL);
        RandomGenerator repeatedGenerator = Create(404UL);
        int fillCounter = 0;
        int repeatedCounter = 0;
        MixtureSampler filling = new MixtureSampler(
            fillGenerator,
            [() => 1.0, () => 1000.0 + fillCounter++],
            [1.0, 4.0]);
        MixtureSampler repeated = new MixtureSampler(
            repeatedGenerator,
            [() => 1.0, () => 1000.0 + repeatedCounter++],
            [1.0, 4.0]);
        double[] actual = new double[2000];
        double[] expected = new double[2000];
        filling.Fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = repeated.Sample();
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillCounter, Is.EqualTo(repeatedCounter));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(repeatedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ComponentAndWeightArraysAreDefensivelyCopied()
    {
        Func<double>[] components = [() => 10.0, () => 20.0, () => 30.0];
        double[] weights = [1.0, 2.0, 3.0];
        MixtureSampler actual = new MixtureSampler(Create(505UL), components, weights);
        MixtureSampler expected = new MixtureSampler(Create(505UL), [() => 10.0, () => 20.0, () => 30.0], [1.0, 2.0, 3.0]);
        Array.Fill(components, () => -999.0);
        Array.Fill(weights, double.NaN);

        for (int index = 0; index < 2000; index++)
        {
            Assert.That(actual.Sample(), Is.EqualTo(expected.Sample()));
        }
    }

    [Test]
    public void SinglePositiveWeightSkipsSelectionEntropyAndPreservesOutput()
    {
        RandomGenerator generator = Create(606UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        int selectedCalls = 0;
        MixtureSampler sampler = new MixtureSampler(
            generator,
            [() => throw new InvalidOperationException(), () => ++selectedCalls, () => throw new InvalidOperationException()],
            [0.0, double.MaxValue, 0.0]);

        double first = sampler.Sample();
        double second = sampler.Sample();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(1.0));
            Assert.That(second, Is.EqualTo(2.0));
            Assert.That(selectedCalls, Is.EqualTo(2));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void ComponentOutputsAreReturnedWithoutValidation()
    {
        MixtureSampler notANumber = new MixtureSampler(Create(707UL), [() => double.NaN], [1.0]);
        MixtureSampler infinity = new MixtureSampler(Create(808UL), [() => double.PositiveInfinity], [1.0]);

        Assert.Multiple(() =>
        {
            Assert.That(double.IsNaN(notANumber.Sample()), Is.True);
            Assert.That(infinity.Sample(), Is.EqualTo(double.PositiveInfinity));
        });
    }

    [Test]
    public void InvalidInputsAreRejectedWithoutConsumingState()
    {
        AssertArgument("generator", () => new MixtureSampler(NullReference.Value<global::random.IRandomSource>(), [() => 1.0], [1.0]));
        RandomGenerator generator = Create(909UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        AssertArgument("components", () => new MixtureSampler(generator, NullReference.Value<global::System.Func<double>[]>(), [1.0]));
        AssertArgument("components", () => new MixtureSampler(generator, [], []));
        AssertArgument("components", () => new MixtureSampler(generator, [() => 1.0, NullReference.Value<global::System.Func<double>>()], [1.0, 1.0]));
        AssertArgument("weights", () => new MixtureSampler(generator, [() => 1.0], NullReference.Value<double[]>()));
        AssertArgument("weights", () => new MixtureSampler(generator, [() => 1.0, () => 2.0], [1.0]));
        AssertArgument("weights", () => new MixtureSampler(generator, [() => 1.0], [-1.0]));
        AssertArgument("weights", () => new MixtureSampler(generator, [() => 1.0], [double.PositiveInfinity]));
        AssertArgument("weights", () => new MixtureSampler(generator, [() => 1.0, () => 2.0], [0.0, 0.0]));
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    static double ReturnAndCount(double value, int[] counts, int index)
    {
        counts[index]++;
        return value;
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
