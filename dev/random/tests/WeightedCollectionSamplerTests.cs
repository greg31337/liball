using System.Collections;

namespace random.Tests;

public class WeightedCollectionSamplerTests
{
    [Test]
    public void WeightedIndexesAndEnumeratedValuesMatchEveryConfiguredMethod()
    {
        WeightedSamplingMethod[] methods =
        [
            WeightedSamplingMethod.Cumulative,
            WeightedSamplingMethod.GuideTable,
            WeightedSamplingMethod.AliasTable
        ];
        double[] weights = [1.0, 3.0, 2.0, 4.0];
        object?[] values = ["zero", "one", "two", "three"];
        foreach (WeightedSamplingMethod method in methods)
        {
            ulong seed = (ulong)(101 + (int)method);
            RandomGenerator actualGenerator = Create(seed);
            RandomGenerator expectedGenerator = Create(seed);
            IWeightedCollectionSampler sampler = new WeightedCollectionSampler(actualGenerator);
            WeightedSampler expectedSampler = WeightedSampler.FromWeights(expectedGenerator, weights, method);

            Assert.That(sampler.WeightedIndex(weights, method), Is.EqualTo(expectedSampler.SampleIndex()));

            int[] actualIndexes = sampler.WeightedIndexes(weights, 37, method);
            int[] expectedIndexes = new int[37];
            expectedSampler.FillIndices(expectedIndexes);
            Assert.That(actualIndexes, Is.EqualTo(expectedIndexes));

            int[] actualFilled = new int[29];
            int[] expectedFilled = new int[29];
            sampler.FillWeightedIndexes(weights, method, actualFilled);
            expectedSampler.FillIndices(expectedFilled);
            Assert.That(actualFilled, Is.EqualTo(expectedFilled));

            object? actualValue = sampler.WeightedValue((IEnumerable)values.Select(value => value), weights, method);
            object? expectedValue = values[expectedSampler.SampleIndex()];
            Assert.That(actualValue, Is.EqualTo(expectedValue));

            object?[] actualValues = sampler.WeightedValues((IEnumerable)values.Select(value => value), weights, 31, method);
            int[] expectedValueIndexes = new int[31];
            expectedSampler.FillIndices(expectedValueIndexes);
            Assert.Multiple(() =>
            {
                Assert.That(actualValues, Is.EqualTo(Select(values, expectedValueIndexes)));
                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void WeightedSamplingWithoutReplacementMatchesExponentialKeyOrdering()
    {
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator expectedGenerator = Create(202UL);
        IWeightedCollectionSampler sampler = new WeightedCollectionSampler(actualGenerator);
        double[] weights = [double.PositiveInfinity, 5.0, 0.0, 3.0, double.PositiveInfinity, 1.0];
        object?[] values = ["a", "b", "c", "d", "e", "f"];

        int[] actualIndexes = sampler.WeightedIndexesWithoutReplacement(weights, 4);
        int[] expectedIndexes = RandomCollections.WeightedSampleWithoutReplacement(expectedGenerator, weights, 4);
        int[] actualFilled = new int[4];
        int[] expectedFilled = new int[4];
        sampler.FillWeightedIndexesWithoutReplacement(weights, actualFilled);
        RandomCollections.FillWeightedSampleWithoutReplacement(expectedGenerator, weights, expectedFilled);
        object?[] actualValues = sampler.WeightedValuesWithoutReplacement((IEnumerable)values.Select(value => value), weights, 4);
        int[] expectedValueIndexes = RandomCollections.WeightedSampleWithoutReplacement(expectedGenerator, weights, 4);

        Assert.Multiple(() =>
        {
            Assert.That(actualIndexes, Is.EqualTo(expectedIndexes));
            Assert.That(actualFilled, Is.EqualTo(expectedFilled));
            Assert.That(actualValues, Is.EqualTo(Select(values, expectedValueIndexes)));
            Assert.That(actualIndexes.Distinct().Count(), Is.EqualTo(actualIndexes.Length));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void BootstrapIndexesAndAllValueSourcesReplayExactly()
    {
        RandomGenerator actualGenerator = Create(303UL);
        RandomGenerator expectedGenerator = Create(303UL);
        IWeightedCollectionSampler sampler = new WeightedCollectionSampler(actualGenerator);
        int[] values = [10, 20, 30, 40, 50];
        ArrayList list = new ArrayList(values);

        Assert.That(sampler.BootstrapIndexes(values.Length, 41), Is.EqualTo(RandomCollections.BootstrapIndexes(expectedGenerator, values.Length, 41)));
        int[] actualFilled = new int[37];
        int[] expectedFilled = new int[37];
        sampler.FillBootstrapIndexes(values.Length, actualFilled);
        RandomCollections.FillBootstrapIndexes(expectedGenerator, values.Length, expectedFilled);

        Array actualArray = sampler.BootstrapArray(values, 23);
        Array expectedArray = RandomCollections.BootstrapSample(expectedGenerator, (Array)values, 23);
        object?[] actualList = sampler.BootstrapList(list, 19);
        object?[] expectedList = RandomCollections.BootstrapSample(expectedGenerator, (IList)list, 19);
        object?[] actualValues = sampler.BootstrapValues((IEnumerable)values.Where(value => value > 0), 17);
        int[] expectedValueIndexes = RandomCollections.BootstrapIndexes(expectedGenerator, values.Length, 17);

        Assert.Multiple(() =>
        {
            Assert.That(actualFilled, Is.EqualTo(expectedFilled));
            Assert.That(actualArray, Is.TypeOf<int[]>());
            Assert.That(actualArray, Is.EqualTo(expectedArray));
            Assert.That(actualList, Is.EqualTo(expectedList));
            Assert.That(actualValues, Is.EqualTo(Select(ToObjects(values), expectedValueIndexes)));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void StratifiedSystematicAndResidualIndexAndValueFormsReplayExactly()
    {
        RandomGenerator actualGenerator = Create(404UL);
        RandomGenerator expectedGenerator = Create(404UL);
        IWeightedCollectionSampler sampler = new WeightedCollectionSampler(actualGenerator);
        double[] weights = [0.5, 1.5, 3.0, 2.0];
        object?[] values = ["north", "south", "east", "west"];

        Assert.That(sampler.StratifiedIndexes(weights, 43), Is.EqualTo(RandomCollections.StratifiedResample(expectedGenerator, weights, 43)));
        int[] actualStratified = new int[31];
        int[] expectedStratified = new int[31];
        sampler.FillStratifiedIndexes(weights, actualStratified);
        RandomCollections.FillStratifiedResample(expectedGenerator, weights, expectedStratified);
        object?[] stratifiedValues = sampler.StratifiedValues((IEnumerable)values.Select(value => value), weights, 27);
        int[] stratifiedIndexes = RandomCollections.StratifiedResample(expectedGenerator, weights, 27);

        Assert.That(sampler.SystematicIndexes(weights, 43), Is.EqualTo(RandomCollections.SystematicResample(expectedGenerator, weights, 43)));
        int[] actualSystematic = new int[31];
        int[] expectedSystematic = new int[31];
        sampler.FillSystematicIndexes(weights, actualSystematic);
        RandomCollections.FillSystematicResample(expectedGenerator, weights, expectedSystematic);
        object?[] systematicValues = sampler.SystematicValues((IEnumerable)values.Select(value => value), weights, 27);
        int[] systematicIndexes = RandomCollections.SystematicResample(expectedGenerator, weights, 27);

        Assert.That(sampler.ResidualIndexes(weights, 43), Is.EqualTo(RandomCollections.ResidualResample(expectedGenerator, weights, 43)));
        int[] actualResidual = new int[31];
        int[] expectedResidual = new int[31];
        sampler.FillResidualIndexes(weights, actualResidual);
        RandomCollections.FillResidualResample(expectedGenerator, weights, expectedResidual);
        object?[] residualValues = sampler.ResidualValues((IEnumerable)values.Select(value => value), weights, 27);
        int[] residualIndexes = RandomCollections.ResidualResample(expectedGenerator, weights, 27);

        Assert.Multiple(() =>
        {
            Assert.That(actualStratified, Is.EqualTo(expectedStratified));
            Assert.That(stratifiedValues, Is.EqualTo(Select(values, stratifiedIndexes)));
            Assert.That(actualSystematic, Is.EqualTo(expectedSystematic));
            Assert.That(systematicValues, Is.EqualTo(Select(values, systematicIndexes)));
            Assert.That(actualResidual, Is.EqualTo(expectedResidual));
            Assert.That(residualValues, Is.EqualTo(Select(values, residualIndexes)));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void DegenerateOutputsRemainDeterministicAndConsumeNoState()
    {
        RandomGenerator generator = Create(505UL);
        IWeightedCollectionSampler sampler = new WeightedCollectionSampler(generator);
        byte[] before = generator.ExportState().ToBytes();

        Assert.That(sampler.WeightedIndexes([1.0], 0, WeightedSamplingMethod.AliasTable), Is.Empty);
        sampler.FillWeightedIndexes([1.0], WeightedSamplingMethod.GuideTable, Span<int>.Empty);
        Assert.That(sampler.WeightedIndexesWithoutReplacement([1.0], 0), Is.Empty);
        sampler.FillWeightedIndexesWithoutReplacement([1.0], Span<int>.Empty);
        Assert.That(sampler.BootstrapIndexes(1, 0), Is.Empty);
        sampler.FillBootstrapIndexes(1, Span<int>.Empty);
        Assert.That(sampler.StratifiedIndexes([1.0], 0), Is.Empty);
        Assert.That(sampler.SystematicIndexes([1.0], 0), Is.Empty);
        Assert.That(sampler.ResidualIndexes([1.0], 0), Is.Empty);

        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void InvalidRequestsAreRejectedWithoutRandomStateOrDestinationChanges()
    {
        AssertArgument("generator", () => new WeightedCollectionSampler(NullReference.Value<global::random.IRandomSource>()));
        RandomGenerator generator = Create(606UL);
        generator.NextBoolean();
        IWeightedCollectionSampler sampler = new WeightedCollectionSampler(generator);
        byte[] before = generator.ExportState().ToBytes();
        int[] destination = [91, 92];

        AssertArgument("method", () => sampler.WeightedIndex([1.0], (WeightedSamplingMethod)99));
        AssertArgument("weights", () => sampler.WeightedIndexes([1.0, -1.0], 2, WeightedSamplingMethod.Cumulative));
        AssertArgument("count", () => sampler.WeightedValues((IEnumerable)new int[] { 1 }, [1.0], -1, WeightedSamplingMethod.AliasTable));
        AssertArgument("weights", () => sampler.WeightedValue((IEnumerable)new int[] { 1, 2 }, [1.0], WeightedSamplingMethod.GuideTable));
        AssertArgument("count", () => sampler.FillWeightedIndexesWithoutReplacement([1.0, 0.0], destination));
        AssertArgument("populationSize", () => sampler.BootstrapIndexes(0, 1));
        AssertArgument("values", () => sampler.BootstrapValues((IEnumerable)Array.Empty<int>(), 0));
        AssertArgument("weights", () => sampler.StratifiedValues((IEnumerable)new int[] { 1, 2 }, [1.0], 2));
        AssertArgument("weights", () => sampler.SystematicIndexes([double.NaN], 1));
        AssertArgument("weights", () => sampler.ResidualIndexes([0.0], 1));

        Assert.Multiple(() =>
        {
            Assert.That(destination, Is.EqualTo(new int[] { 91, 92 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    static object?[] Select(object?[] values, int[] indexes)
    {
        object?[] result = new object?[indexes.Length];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = values[indexes[index]];
        }

        return result;
    }

    static object?[] ToObjects(int[] values)
    {
        object?[] result = new object?[values.Length];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = values[index];
        }

        return result;
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
