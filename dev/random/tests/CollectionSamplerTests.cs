using System.Collections;

namespace random.Tests;

public class CollectionSamplerTests
{
    [Test]
    public void ArrayAndListChoicesMatchTheValidatedCollectionAlgorithms()
    {
        RandomGenerator actualGenerator = Create(101UL);
        RandomGenerator expectedGenerator = Create(101UL);
        ICollectionSampler sampler = new CollectionSampler(actualGenerator);
        int[] arrayValues = [10, 20, 30, 40];
        ArrayList listValues = new ArrayList() { 10, 20, 30, 40 };
        int[] actualArrayChoices = new int[19];
        int[] expectedArrayChoices = new int[19];
        int[] actualListChoices = new int[17];
        int[] expectedListChoices = new int[17];

        Assert.That(sampler.ChooseFromArray(arrayValues), Is.EqualTo(RandomCollections.Choose(expectedGenerator, arrayValues)));
        Assert.That(sampler.ChooseFromList(listValues), Is.EqualTo(RandomCollections.Choose(expectedGenerator, listValues)));
        sampler.FillChoicesFromArray(arrayValues, actualArrayChoices);
        RandomCollections.FillChoices(expectedGenerator, arrayValues, expectedArrayChoices);
        sampler.FillChoicesFromList(listValues, actualListChoices);
        RandomCollections.FillChoices(expectedGenerator, listValues, expectedListChoices);

        Assert.Multiple(() =>
        {
            Assert.That(actualArrayChoices, Is.EqualTo(expectedArrayChoices));
            Assert.That(actualListChoices, Is.EqualTo(expectedListChoices));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void InPlaceAndCopyShufflesMatchForArraysAndLists()
    {
        RandomGenerator actualGenerator = Create(202UL);
        RandomGenerator expectedGenerator = Create(202UL);
        ICollectionSampler sampler = new CollectionSampler(actualGenerator);
        int[] actualArray = Enumerable.Range(0, 20).ToArray();
        int[] expectedArray = Enumerable.Range(0, 20).ToArray();
        ArrayList actualList = new ArrayList(Enumerable.Range(0, 20).ToArray());
        ArrayList expectedList = new ArrayList(Enumerable.Range(0, 20).ToArray());

        sampler.ShuffleArray(actualArray);
        RandomCollections.Shuffle(expectedGenerator, expectedArray);
        sampler.ShuffleList(actualList);
        RandomCollections.Shuffle(expectedGenerator, expectedList);
        sampler.PartialShuffleArray(actualArray, 7);
        RandomCollections.PartialShuffle(expectedGenerator, expectedArray, 7);
        sampler.PartialShuffleList(actualList, 7);
        RandomCollections.PartialShuffle(expectedGenerator, expectedList, 7);

        int[] arraySource = Enumerable.Range(30, 12).ToArray();
        ArrayList listSource = new ArrayList(arraySource);
        Array actualArrayCopy = sampler.ShuffledArrayCopy(arraySource);
        Array expectedArrayCopy = RandomCollections.ShuffledCopy(expectedGenerator, arraySource);
        object?[] actualListCopy = sampler.ShuffledListCopy(listSource);
        object?[] expectedListCopy = RandomCollections.ShuffledCopy(expectedGenerator, listSource);

        Assert.Multiple(() =>
        {
            Assert.That(actualArray, Is.EqualTo(expectedArray));
            Assert.That(actualList, Is.EqualTo(expectedList));
            Assert.That(actualArrayCopy, Is.TypeOf<int[]>());
            Assert.That(actualArrayCopy, Is.EqualTo(expectedArrayCopy));
            Assert.That(actualListCopy, Is.EqualTo(expectedListCopy));
            Assert.That(arraySource, Is.EqualTo(Enumerable.Range(30, 12).ToArray()));
            Assert.That(listSource, Is.EqualTo(new ArrayList(arraySource)));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void IndexAndWithoutReplacementOperationsExposeTheExistingExactAlgorithms()
    {
        RandomGenerator actualGenerator = Create(303UL);
        RandomGenerator expectedGenerator = Create(303UL);
        ICollectionSampler sampler = new CollectionSampler(actualGenerator);
        int[] values = Enumerable.Range(100, 30).ToArray();
        ArrayList list = new ArrayList(values);

        Assert.That(sampler.Permutation(17), Is.EqualTo(RandomCollections.Permutation(expectedGenerator, 17)));
        Assert.That(sampler.Subset(31, 9), Is.EqualTo(RandomCollections.Subset(expectedGenerator, 31, 9)));
        Assert.That(sampler.Combination(29, 8), Is.EqualTo(RandomCollections.Combination(expectedGenerator, 29, 8)));
        Assert.That(sampler.Derangement(12), Is.EqualTo(RandomCollections.Derangement(expectedGenerator, 12)));
        Array arraySample = sampler.SampleArrayWithoutReplacement(values, 11);
        Array expectedArraySample = RandomCollections.SampleWithoutReplacement(expectedGenerator, (Array)values, 11);
        object?[] listSample = sampler.SampleListWithoutReplacement(list, 11);
        object?[] expectedListSample = RandomCollections.SampleWithoutReplacement(expectedGenerator, (IList)list, 11);

        Assert.Multiple(() =>
        {
            Assert.That(arraySample, Is.TypeOf<int[]>());
            Assert.That(arraySample, Is.EqualTo(expectedArraySample));
            Assert.That(listSample, Is.EqualTo(expectedListSample));
            Assert.That(values, Is.EqualTo(Enumerable.Range(100, 30).ToArray()));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void StreamingAndListReservoirSamplesUseAlgorithmR()
    {
        RandomGenerator actualGenerator = Create(404UL);
        RandomGenerator expectedGenerator = Create(404UL);
        ICollectionSampler sampler = new CollectionSampler(actualGenerator);
        int[] values = Enumerable.Range(0, 80).ToArray();
        ArrayList list = new ArrayList(values);

        object?[] actualStream = sampler.ReservoirSample((IEnumerable)values, 13);
        object?[] expectedStream = RandomCollections.ReservoirSample(expectedGenerator, (IEnumerable)values, 13);
        object?[] actualList = sampler.ReservoirSampleList(list, 11);
        object?[] expectedList = RandomCollections.ReservoirSample(expectedGenerator, (IList)list, 11);
        byte[] beforeEmpty = actualGenerator.ExportState().ToBytes();
        object?[] empty = sampler.ReservoirSample((IEnumerable)values, 0);

        Assert.Multiple(() =>
        {
            Assert.That(actualStream, Is.EqualTo(expectedStream));
            Assert.That(actualList, Is.EqualTo(expectedList));
            Assert.That(empty, Is.Empty);
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(beforeEmpty));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void TypedArrayChoicesPreserveStaticValueTypesAndReplayOrder()
    {
        RandomGenerator actualGenerator = Create(505UL);
        RandomGenerator expectedGenerator = Create(505UL);
        ICollectionSampler sampler = new CollectionSampler(actualGenerator);

        Assert.That(sampler.ChooseBooleanArray([false, true]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new bool[] { false, true })));
        Assert.That(sampler.ChooseByteArray([1, 2]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new byte[] { 1, 2 })));
        Assert.That(sampler.ChooseSByteArray([-1, 2]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new sbyte[] { -1, 2 })));
        Assert.That(sampler.ChooseInt16Array([-3, 4]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new short[] { -3, 4 })));
        Assert.That(sampler.ChooseUInt16Array([5, 6]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new ushort[] { 5, 6 })));
        Assert.That(sampler.ChooseInt32Array([-7, 8]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new int[] { -7, 8 })));
        Assert.That(sampler.ChooseUInt32Array([9U, 10U]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new uint[] { 9U, 10U })));
        Assert.That(sampler.ChooseInt64Array([-11L, 12L]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new long[] { -11L, 12L })));
        Assert.That(sampler.ChooseUInt64Array([13UL, 14UL]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new ulong[] { 13UL, 14UL })));
        Assert.That(sampler.ChooseCharacterArray(['a', 'b']), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new char[] { 'a', 'b' })));
        Assert.That(sampler.ChooseSingleArray([1.5f, 2.5f]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new float[] { 1.5f, 2.5f })));
        Assert.That(sampler.ChooseDoubleArray([3.5, 4.5]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new double[] { 3.5, 4.5 })));
        Assert.That(sampler.ChooseDecimalArray([5.5m, 6.5m]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new decimal[] { 5.5m, 6.5m })));
        Assert.That(sampler.ChooseStringArray(["left", "right"]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new string[] { "left", "right" })));
        Assert.That(sampler.ChooseObjectArray(["value", 17]), Is.EqualTo(RandomCollections.Choose(expectedGenerator, new object[] { "value", 17 })));
        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(expectedGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void InvalidRequestsAreRejectedBeforeRandomStateOrInputsChange()
    {
        AssertArgument("generator", () => new CollectionSampler(NullReference.Value<global::random.IRandomSource>()));
        RandomGenerator generator = Create(606UL);
        generator.NextBoolean();
        ICollectionSampler sampler = new CollectionSampler(generator);
        byte[] before = generator.ExportState().ToBytes();
        string[] incompatibleDestination = ["unchanged", "unchanged"];
        int[] partialValues = [1, 2, 3];
        ArrayList readOnly = ArrayList.ReadOnly(new ArrayList() { 1, 2, 3 });

        AssertArgument("values", () => sampler.ChooseFromArray(Array.Empty<int>()));
        AssertArgument("values", () => sampler.ChooseFromList(new ArrayList()));
        AssertArgument("destination", () => sampler.FillChoicesFromArray(new object[] { "valid", 1 }, incompatibleDestination));
        AssertArgument("count", () => sampler.PartialShuffleArray(partialValues, 4));
        AssertArgument("values", () => sampler.ShuffleList(readOnly));
        AssertArgument("count", () => sampler.SampleArrayWithoutReplacement(partialValues, 4));
        AssertArgument("count", () => sampler.ReservoirSample((IEnumerable)new int[] { 1, 2 }, 3));
        AssertArgument("count", () => sampler.Derangement(1));

        Assert.Multiple(() =>
        {
            Assert.That(incompatibleDestination, Is.EqualTo(new string[] { "unchanged", "unchanged" }));
            Assert.That(partialValues, Is.EqualTo(new int[] { 1, 2, 3 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
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
