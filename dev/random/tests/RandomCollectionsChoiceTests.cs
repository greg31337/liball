using System.Collections;

namespace random.Tests;

public class RandomCollectionsChoiceTests
{
    [Test]
    public void ArrayAndListChoicesMatchUniformIndexDrawsExactly()
    {
        Array arrayValues = Array.CreateInstance(typeof(string), [4], [-2]);
        arrayValues.SetValue("zero", -2);
        arrayValues.SetValue("one", -1);
        arrayValues.SetValue("two", 0);
        arrayValues.SetValue("three", 1);
        IList listValues = new ArrayList() { "zero", "one", "two", "three" };
        RandomGenerator arrayGenerator = Create(101UL);
        RandomGenerator arrayReference = Create(101UL);
        RandomGenerator listGenerator = Create(202UL);
        RandomGenerator listReference = Create(202UL);

        for (int draw = 0; draw < 1000; draw++)
        {
            int arrayIndex = arrayReference.NextInt32Bounded(4);
            int listIndex = listReference.NextInt32Bounded(4);
            Assert.That(RandomCollections.Choose(arrayGenerator, arrayValues), Is.EqualTo(arrayValues.GetValue(arrayIndex - 2)));
            Assert.That(RandomCollections.Choose(listGenerator, listValues), Is.EqualTo(listValues[listIndex]));
        }

        Assert.Multiple(() =>
        {
            Assert.That(arrayGenerator.ExportState().ToBytes(), Is.EqualTo(arrayReference.ExportState().ToBytes()));
            Assert.That(listGenerator.ExportState().ToBytes(), Is.EqualTo(listReference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ExplicitChoiceAndFillOverloadsPreserveElementTypes()
    {
        bool[] booleans = new bool[2];
        byte[] bytes = new byte[2];
        sbyte[] signedBytes = new sbyte[2];
        short[] shorts = new short[2];
        ushort[] unsignedShorts = new ushort[2];
        int[] integers = new int[2];
        uint[] unsignedIntegers = new uint[2];
        long[] longs = new long[2];
        ulong[] unsignedLongs = new ulong[2];
        char[] characters = new char[2];
        float[] singles = new float[2];
        double[] doubles = new double[2];
        decimal[] decimals = new decimal[2];
        string[] strings = new string[2];
        object[] objects = new object[2];

        RandomCollections.FillChoices(Create(303UL), new bool[] { true }, booleans);
        RandomCollections.FillChoices(Create(303UL), new byte[] { 7 }, bytes);
        RandomCollections.FillChoices(Create(303UL), new sbyte[] { -7 }, signedBytes);
        RandomCollections.FillChoices(Create(303UL), new short[] { -17 }, shorts);
        RandomCollections.FillChoices(Create(303UL), new ushort[] { 17 }, unsignedShorts);
        RandomCollections.FillChoices(Create(303UL), new int[] { -27 }, integers);
        RandomCollections.FillChoices(Create(303UL), new uint[] { 27 }, unsignedIntegers);
        RandomCollections.FillChoices(Create(303UL), new long[] { -37 }, longs);
        RandomCollections.FillChoices(Create(303UL), new ulong[] { 37 }, unsignedLongs);
        RandomCollections.FillChoices(Create(303UL), new char[] { 'x' }, characters);
        RandomCollections.FillChoices(Create(303UL), new float[] { 1.25f }, singles);
        RandomCollections.FillChoices(Create(303UL), new double[] { 2.5 }, doubles);
        RandomCollections.FillChoices(Create(303UL), new decimal[] { 3.75m }, decimals);
        RandomCollections.FillChoices(Create(303UL), new string[] { "value" }, strings);
        RandomCollections.FillChoices(Create(303UL), new object[] { "object" }, objects);

        Assert.Multiple(() =>
        {
            Assert.That(RandomCollections.Choose(Create(1UL), new bool[] { true }), Is.True);
            Assert.That(RandomCollections.Choose(Create(1UL), new byte[] { 7 }), Is.EqualTo((byte)7));
            Assert.That(RandomCollections.Choose(Create(1UL), new sbyte[] { -7 }), Is.EqualTo((sbyte)-7));
            Assert.That(RandomCollections.Choose(Create(1UL), new short[] { -17 }), Is.EqualTo((short)-17));
            Assert.That(RandomCollections.Choose(Create(1UL), new ushort[] { 17 }), Is.EqualTo((ushort)17));
            Assert.That(RandomCollections.Choose(Create(1UL), new int[] { -27 }), Is.EqualTo(-27));
            Assert.That(RandomCollections.Choose(Create(1UL), new uint[] { 27 }), Is.EqualTo(27U));
            Assert.That(RandomCollections.Choose(Create(1UL), new long[] { -37 }), Is.EqualTo(-37L));
            Assert.That(RandomCollections.Choose(Create(1UL), new ulong[] { 37 }), Is.EqualTo(37UL));
            Assert.That(RandomCollections.Choose(Create(1UL), new char[] { 'x' }), Is.EqualTo('x'));
            Assert.That(RandomCollections.Choose(Create(1UL), new float[] { 1.25f }), Is.EqualTo(1.25f));
            Assert.That(RandomCollections.Choose(Create(1UL), new double[] { 2.5 }), Is.EqualTo(2.5));
            Assert.That(RandomCollections.Choose(Create(1UL), new decimal[] { 3.75m }), Is.EqualTo(3.75m));
            Assert.That(RandomCollections.Choose(Create(1UL), new string[] { "value" }), Is.EqualTo("value"));
            Assert.That(RandomCollections.Choose(Create(1UL), new object[] { "object" }), Is.EqualTo("object"));
            Assert.That(booleans, Is.All.True);
            Assert.That(bytes, Is.All.EqualTo(7));
            Assert.That(signedBytes, Is.All.EqualTo(-7));
            Assert.That(shorts, Is.All.EqualTo(-17));
            Assert.That(unsignedShorts, Is.All.EqualTo(17));
            Assert.That(integers, Is.All.EqualTo(-27));
            Assert.That(unsignedIntegers, Is.All.EqualTo(27));
            Assert.That(longs, Is.All.EqualTo(-37));
            Assert.That(unsignedLongs, Is.All.EqualTo(37));
            Assert.That(characters, Is.All.EqualTo('x'));
            Assert.That(singles, Is.All.EqualTo(1.25f));
            Assert.That(doubles, Is.All.EqualTo(2.5));
            Assert.That(decimals, Is.All.EqualTo(3.75m));
            Assert.That(strings, Is.All.EqualTo("value"));
            Assert.That(objects, Is.All.EqualTo("object"));
        });
    }

    [Test]
    public void TypedChoiceFillMatchesRepeatedScalarChoicesExactly()
    {
        int[] values = [10, 20, 30, 40, 50];
        int[] actual = new int[257];
        int[] expected = new int[257];
        RandomGenerator fillGenerator = Create(404UL);
        RandomGenerator scalarGenerator = Create(404UL);

        RandomCollections.FillChoices(fillGenerator, values, actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = RandomCollections.Choose(scalarGenerator, values);
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ChoiceFillsCaptureSelectionsBeforeAliasingWrites()
    {
        int[] values = [10, 20, 30, 40, 50, 60];
        int[] original = (int[])values.Clone();
        int[] expected = new int[values.Length];
        RandomGenerator actualGenerator = Create(505UL);
        RandomGenerator referenceGenerator = Create(505UL);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = original[referenceGenerator.NextInt32Bounded(original.Length)];
        }

        RandomCollections.FillChoices(actualGenerator, values, values);

        Assert.Multiple(() =>
        {
            Assert.That(values, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void GeneralChoiceFillsSupportListSourcesAndArbitraryArrayBounds()
    {
        IList values = new ArrayList() { "a", "b", "c", "d" };
        Array destination = Array.CreateInstance(typeof(object), [31], [5]);
        object?[] expected = new object?[31];
        RandomGenerator actualGenerator = Create(606UL);
        RandomGenerator referenceGenerator = Create(606UL);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = values[referenceGenerator.NextInt32Bounded(values.Count)];
        }

        RandomCollections.FillChoices(actualGenerator, values, destination);

        object?[] actual = new object?[31];
        for (int index = 0; index < actual.Length; index++)
        {
            actual[index] = destination.GetValue(index + 5);
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ShuffledCopiesMatchFisherYatesWithoutMutatingSources()
    {
        int[] source = [0, 1, 2, 3, 4, 5, 6];
        int[] original = (int[])source.Clone();
        int[] expected = (int[])source.Clone();
        RandomGenerator actualGenerator = Create(707UL);
        RandomGenerator referenceGenerator = Create(707UL);
        ReferenceShuffle(referenceGenerator, expected);

        int[] actual = RandomCollections.ShuffledCopy(actualGenerator, source);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actual, Is.Not.SameAs(source));
            Assert.That(source, Is.EqualTo(original));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void GeneralShuffledCopiesPreserveArrayBoundsAndListElements()
    {
        Array source = Array.CreateInstance(typeof(int), [5], [-4]);
        for (int index = 0; index < source.Length; index++)
        {
            source.SetValue(index, index - 4);
        }

        Array sourceSnapshot = (Array)source.Clone();
        Array arrayCopy = RandomCollections.ShuffledCopy(Create(808UL), source);
        IList list = new ArrayList() { null, "a", "b", "c" };
        object?[] listCopy = RandomCollections.ShuffledCopy(Create(909UL), list);

        Assert.Multiple(() =>
        {
            Assert.That(arrayCopy.GetLowerBound(0), Is.EqualTo(-4));
            Assert.That(arrayCopy.GetUpperBound(0), Is.EqualTo(0));
            Assert.That(arrayCopy.Cast<int>(), Is.EquivalentTo(Enumerable.Range(0, 5)));
            Assert.That(source.Cast<int>(), Is.EqualTo(sourceSnapshot.Cast<int>()));
            Assert.That(listCopy, Is.EquivalentTo(list.Cast<object>()));
            Assert.That(list, Is.EqualTo(new ArrayList() { null, "a", "b", "c" }));
        });
    }

    [Test]
    public void ExplicitShuffledCopyOverloadsRetainArrayTypes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new bool[] { false, true }), Is.TypeOf<bool[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new byte[] { 0, 1 }), Is.TypeOf<byte[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new sbyte[] { 0, 1 }), Is.TypeOf<sbyte[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new short[] { 0, 1 }), Is.TypeOf<short[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new ushort[] { 0, 1 }), Is.TypeOf<ushort[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new int[] { 0, 1 }), Is.TypeOf<int[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new uint[] { 0, 1 }), Is.TypeOf<uint[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new long[] { 0, 1 }), Is.TypeOf<long[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new ulong[] { 0, 1 }), Is.TypeOf<ulong[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new char[] { 'a', 'b' }), Is.TypeOf<char[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new float[] { 0, 1 }), Is.TypeOf<float[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new double[] { 0, 1 }), Is.TypeOf<double[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new decimal[] { 0, 1 }), Is.TypeOf<decimal[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new string[] { "a", "b" }), Is.TypeOf<string[]>());
            Assert.That(RandomCollections.ShuffledCopy(Create(1UL), new object[] { "a", "b" }), Is.TypeOf<object[]>());
        });
    }

    [Test]
    public void PartialShuffleMatchesAscendingReferenceAndShuffleAlias()
    {
        int[] partial = Enumerable.Range(0, 13).ToArray();
        int[] alias = Enumerable.Range(0, 13).ToArray();
        int[] expected = Enumerable.Range(0, 13).ToArray();
        RandomGenerator partialGenerator = Create(1001UL);
        RandomGenerator aliasGenerator = Create(1001UL);
        RandomGenerator referenceGenerator = Create(1001UL);

        RandomCollections.PartialShuffle(partialGenerator, partial, 5);
        RandomCollections.Shuffle(aliasGenerator, alias, 5);
        ReferencePartialShuffle(referenceGenerator, expected, 5);

        Assert.Multiple(() =>
        {
            Assert.That(partial, Is.EqualTo(expected));
            Assert.That(alias, Is.EqualTo(expected));
            Assert.That(partial, Is.EquivalentTo(Enumerable.Range(0, 13)));
            Assert.That(partialGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            Assert.That(aliasGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void PartialShuffleSupportsGeneralArraysAndListsExactly()
    {
        Array arrayValues = Array.CreateInstance(typeof(int), [8], [-3]);
        for (int index = 0; index < arrayValues.Length; index++)
        {
            arrayValues.SetValue(index, index - 3);
        }

        IList listValues = new ArrayList(Enumerable.Range(0, 8).ToArray());
        int[] expected = Enumerable.Range(0, 8).ToArray();
        RandomGenerator arrayGenerator = Create(1102UL);
        RandomGenerator listGenerator = Create(1102UL);
        RandomGenerator referenceGenerator = Create(1102UL);

        RandomCollections.PartialShuffle(arrayGenerator, arrayValues, 4);
        RandomCollections.PartialShuffle(listGenerator, listValues, 4);
        ReferencePartialShuffle(referenceGenerator, expected, 4);

        Assert.Multiple(() =>
        {
            Assert.That(arrayValues.Cast<int>(), Is.EqualTo(expected));
            Assert.That(listValues.Cast<int>(), Is.EqualTo(expected));
            Assert.That(arrayGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            Assert.That(listGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void SmallPartialShufflesReachEveryOrderedPrefix()
    {
        RandomGenerator generator = Create(1203UL);
        HashSet<string> prefixes = new HashSet<string>();
        for (int attempt = 0; attempt < 3000; attempt++)
        {
            int[] values = [0, 1, 2, 3, 4];
            RandomCollections.PartialShuffle(generator, values, 3);
            prefixes.Add(string.Join(",", values.Take(3)));
        }

        Assert.That(prefixes.Count, Is.EqualTo(60));
    }

    [Test]
    public void DeterministicChoiceOperationsConsumeNoState()
    {
        RandomGenerator generator = Create(1304UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        int[] singleton = [42];
        int[] destination = new int[8];
        int[] empty = [];

        int choice = RandomCollections.Choose(generator, singleton);
        RandomCollections.FillChoices(generator, singleton, destination);
        RandomCollections.FillChoices(generator, singleton, empty);
        int[] copy = RandomCollections.ShuffledCopy(generator, singleton);
        RandomCollections.PartialShuffle(generator, singleton, 0);
        RandomCollections.PartialShuffle(generator, singleton, 1);
        RandomCollections.Shuffle(generator, empty, 0);

        Assert.Multiple(() =>
        {
            Assert.That(choice, Is.EqualTo(42));
            Assert.That(destination, Is.All.EqualTo(42));
            Assert.That(copy, Is.EqualTo(singleton));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidChoiceInputsFailBeforeMutationOrRandomConsumption()
    {
        RandomGenerator generator = Create(1405UL);
        byte[] before = generator.ExportState().ToBytes();
        string[] incompatibleDestination = ["unchanged", "unchanged"];
        int[] partialValues = [0, 1, 2, 3];
        IList readOnly = ArrayList.ReadOnly(new ArrayList() { 0, 1, 2, 3 });
        Array rectangular = new int[2, 2];

        object[] cases =
        [
            new object[] { (TestDelegate)(() => RandomCollections.Choose(generator, Array.Empty<int>())), "values" },
            new object[] { (TestDelegate)(() => RandomCollections.Choose(generator, rectangular)), "values" },
            new object[] { (TestDelegate)(() => RandomCollections.FillChoices(generator, (Array)new object[] { "valid", 1 }, incompatibleDestination)), "destination" },
            new object[] { (TestDelegate)(() => RandomCollections.FillChoices(generator, (Array)new int[] { 1 }, rectangular)), "destination" },
            new object[] { (TestDelegate)(() => RandomCollections.PartialShuffle(generator, partialValues, -1)), "count" },
            new object[] { (TestDelegate)(() => RandomCollections.PartialShuffle(generator, partialValues, 5)), "count" },
            new object[] { (TestDelegate)(() => RandomCollections.PartialShuffle(generator, readOnly, 2)), "values" },
            new object[] { (TestDelegate)(() => RandomCollections.PartialShuffle(generator, rectangular, 2)), "values" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }

        Assert.Multiple(() =>
        {
            Assert.That(incompatibleDestination, Is.EqualTo(new string[] { "unchanged", "unchanged" }));
            Assert.That(partialValues, Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(readOnly.Cast<int>(), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void NullChoiceArgumentsIdentifyTheFailingParameter()
    {
        RandomGenerator generator = Create(1506UL);
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => RandomCollections.Choose(NullReference.Value<global::random.IRandomSource>(), new int[] { 1 })), "generator" },
            new object[] { (TestDelegate)(() => RandomCollections.Choose(generator, (int[])NullReference.Value<int[]>())), "values" },
            new object[] { (TestDelegate)(() => RandomCollections.FillChoices(generator, new int[] { 1 }, (int[])NullReference.Value<int[]>())), "destination" },
            new object[] { (TestDelegate)(() => RandomCollections.ShuffledCopy(generator, (Array)NullReference.Value<Array>())), "values" },
            new object[] { (TestDelegate)(() => RandomCollections.PartialShuffle(generator, (IList)NullReference.Value<global::System.Collections.IList>(), 0)), "values" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }

        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    static void ReferenceShuffle(RandomGenerator generator, int[] values)
    {
        for (int current = values.Length - 1; current > 0; current--)
        {
            int selected = generator.NextInt32Bounded(current + 1);
            int temporary = values[current];
            values[current] = values[selected];
            values[selected] = temporary;
        }
    }

    static void ReferencePartialShuffle(RandomGenerator generator, int[] values, int count)
    {
        for (int current = 0; current < count; current++)
        {
            int selected = current + generator.NextInt32Bounded(values.Length - current);
            int temporary = values[current];
            values[current] = values[selected];
            values[selected] = temporary;
        }
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
