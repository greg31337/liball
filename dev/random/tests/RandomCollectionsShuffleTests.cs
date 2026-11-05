using System.Collections;

namespace random.Tests;

public class RandomCollectionsShuffleTests
{
    [Test]
    public void SmallShufflesReachEveryPermutationThroughLengthSix()
    {
        int[] factorials = [1, 1, 2, 6, 24, 120, 720];
        for (int length = 0; length <= 6; length++)
        {
            RandomGenerator generator = Create((ulong)(1000 + length));
            HashSet<string> permutations = new HashSet<string>();
            int attempts = factorials[length] * 30;
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int[] values = Enumerable.Range(0, length).ToArray();
                RandomCollections.Shuffle(generator, values);
                permutations.Add(string.Join(",", values));
            }

            Assert.That(permutations.Count, Is.EqualTo(factorials[length]), $"length {length}");
        }
    }

    [Test]
    public void TypedArrayShuffleMatchesDescendingFisherYatesExactly()
    {
        RandomGenerator actualGenerator = Create(42UL);
        RandomGenerator referenceGenerator = Create(42UL);

        for (int repetition = 0; repetition < 257; repetition++)
        {
            int[] actual = Enumerable.Range(0, 31).ToArray();
            int[] expected = Enumerable.Range(0, 31).ToArray();
            RandomCollections.Shuffle(actualGenerator, actual);
            ReferenceShuffle(referenceGenerator, expected);
            Assert.That(actual, Is.EqualTo(expected));
        }

        Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void ExplicitArrayOverloadsApplyTheSamePermutation()
    {
        const ulong seed = 123UL;
        int[] order = [0, 1, 2, 3, 4];
        ReferenceShuffle(Create(seed), order);

        bool[] booleanValues = [false, true, false, true, false];
        byte[] byteValues = [0, 1, 2, 3, 4];
        sbyte[] signedByteValues = [0, 1, 2, 3, 4];
        short[] shortValues = [0, 1, 2, 3, 4];
        ushort[] unsignedShortValues = [0, 1, 2, 3, 4];
        int[] integerValues = [0, 1, 2, 3, 4];
        uint[] unsignedIntegerValues = [0, 1, 2, 3, 4];
        long[] longValues = [0, 1, 2, 3, 4];
        ulong[] unsignedLongValues = [0, 1, 2, 3, 4];
        char[] characterValues = ['0', '1', '2', '3', '4'];
        float[] singleValues = [0, 1, 2, 3, 4];
        double[] doubleValues = [0, 1, 2, 3, 4];
        decimal[] decimalValues = [0, 1, 2, 3, 4];
        string[] stringValues = ["0", "1", "2", "3", "4"];
        object[] objectValues = [0, 1, 2, 3, 4];

        RandomCollections.Shuffle(Create(seed), booleanValues);
        RandomCollections.Shuffle(Create(seed), byteValues);
        RandomCollections.Shuffle(Create(seed), signedByteValues);
        RandomCollections.Shuffle(Create(seed), shortValues);
        RandomCollections.Shuffle(Create(seed), unsignedShortValues);
        RandomCollections.Shuffle(Create(seed), integerValues);
        RandomCollections.Shuffle(Create(seed), unsignedIntegerValues);
        RandomCollections.Shuffle(Create(seed), longValues);
        RandomCollections.Shuffle(Create(seed), unsignedLongValues);
        RandomCollections.Shuffle(Create(seed), characterValues);
        RandomCollections.Shuffle(Create(seed), singleValues);
        RandomCollections.Shuffle(Create(seed), doubleValues);
        RandomCollections.Shuffle(Create(seed), decimalValues);
        RandomCollections.Shuffle(Create(seed), stringValues);
        RandomCollections.Shuffle(Create(seed), objectValues);

        Assert.Multiple(() =>
        {
            Assert.That(booleanValues, Is.EqualTo(order.Select(index => index % 2 == 1)));
            Assert.That(byteValues, Is.EqualTo(order.Select(index => (byte)index)));
            Assert.That(signedByteValues, Is.EqualTo(order.Select(index => (sbyte)index)));
            Assert.That(shortValues, Is.EqualTo(order.Select(index => (short)index)));
            Assert.That(unsignedShortValues, Is.EqualTo(order.Select(index => (ushort)index)));
            Assert.That(integerValues, Is.EqualTo(order));
            Assert.That(unsignedIntegerValues, Is.EqualTo(order.Select(index => (uint)index)));
            Assert.That(longValues, Is.EqualTo(order.Select(index => (long)index)));
            Assert.That(unsignedLongValues, Is.EqualTo(order.Select(index => (ulong)index)));
            Assert.That(characterValues, Is.EqualTo(order.Select(index => (char)('0' + index))));
            Assert.That(singleValues, Is.EqualTo(order.Select(index => (float)index)));
            Assert.That(doubleValues, Is.EqualTo(order.Select(index => (double)index)));
            Assert.That(decimalValues, Is.EqualTo(order.Select(index => (decimal)index)));
            Assert.That(stringValues, Is.EqualTo(order.Select(index => index.ToString())));
            Assert.That(objectValues, Is.EqualTo(order.Cast<object>()));
        });
    }

    [Test]
    public void GeneralArrayAndListOverloadsMatchTheSameReferenceTraversal()
    {
        Array arrayValues = new int[] { 0, 1, 2, 3, 4, 5, 6 };
        IList listValues = new ArrayList() { 0, 1, 2, 3, 4, 5, 6 };
        int[] expected = [0, 1, 2, 3, 4, 5, 6];
        RandomGenerator arrayGenerator = Create(234UL);
        RandomGenerator listGenerator = Create(234UL);
        RandomGenerator referenceGenerator = Create(234UL);

        RandomCollections.Shuffle(arrayGenerator, arrayValues);
        RandomCollections.Shuffle(listGenerator, listValues);
        ReferenceShuffle(referenceGenerator, expected);

        Assert.Multiple(() =>
        {
            Assert.That(arrayValues.Cast<int>(), Is.EqualTo(expected));
            Assert.That(listValues.Cast<int>(), Is.EqualTo(expected));
            Assert.That(arrayGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            Assert.That(listGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ArrayOverloadSupportsNonzeroAndNegativeLowerBounds()
    {
        Array values = Array.CreateInstance(typeof(int), [7], [-3]);
        int[] expected = Enumerable.Range(0, 7).ToArray();
        for (int index = 0; index < expected.Length; index++)
        {
            values.SetValue(index, index - 3);
        }

        RandomGenerator actualGenerator = Create(345UL);
        RandomGenerator referenceGenerator = Create(345UL);
        RandomCollections.Shuffle(actualGenerator, values);
        ReferenceShuffle(referenceGenerator, expected);

        int[] actual = new int[7];
        for (int index = 0; index < actual.Length; index++)
        {
            actual[index] = Convert.ToInt32(values.GetValue(index - 3));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void FixedSizeListsRemainShuffleable()
    {
        IList values = ArrayList.FixedSize(new ArrayList() { 0, 1, 2, 3, 4, 5 });
        int[] expected = [0, 1, 2, 3, 4, 5];
        RandomGenerator actualGenerator = Create(456UL);
        RandomGenerator referenceGenerator = Create(456UL);

        RandomCollections.Shuffle(actualGenerator, values);
        ReferenceShuffle(referenceGenerator, expected);

        Assert.Multiple(() =>
        {
            Assert.That(values.Cast<int>(), Is.EqualTo(expected));
            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        });
    }

    [Test]
    public void EmptyAndSingletonCollectionsConsumeNoRandomState()
    {
        RandomGenerator generator = Create(567UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        Array emptyArray = Array.Empty<int>();
        Array singletonArray = new int[] { 1 };
        IList emptyList = new ArrayList();
        IList singletonList = new ArrayList() { 1 };

        RandomCollections.Shuffle(generator, Array.Empty<int>());
        RandomCollections.Shuffle(generator, new string[] { "one" });
        RandomCollections.Shuffle(generator, emptyArray);
        RandomCollections.Shuffle(generator, singletonArray);
        RandomCollections.Shuffle(generator, emptyList);
        RandomCollections.Shuffle(generator, singletonList);

        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void InvalidCollectionsAreRejectedBeforeMutationOrRandomConsumption()
    {
        RandomGenerator generator = Create(678UL);
        byte[] before = generator.ExportState().ToBytes();
        Array rectangular = new int[2, 2] { { 0, 1 }, { 2, 3 } };
        IList readOnly = ArrayList.ReadOnly(new ArrayList() { 0, 1, 2, 3 });

        RandomArgumentException rankError = Assert.Throws<RandomArgumentException>(() => RandomCollections.Shuffle(generator, rectangular));
        RandomArgumentException readOnlyError = Assert.Throws<RandomArgumentException>(() => RandomCollections.Shuffle(generator, readOnly));

        Assert.Multiple(() =>
        {
            Assert.That(rankError.ArgumentName(), Is.EqualTo("values"));
            Assert.That(readOnlyError.ArgumentName(), Is.EqualTo("values"));
            Assert.That(rectangular, Is.EqualTo(new int[2, 2] { { 0, 1 }, { 2, 3 } }));
            Assert.That(readOnly.Cast<int>(), Is.EqualTo(new int[] { 0, 1, 2, 3 }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void NullArgumentsIdentifyTheFailingParameter()
    {
        RandomGenerator generator = Create(789UL);
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => RandomCollections.Shuffle(NullReference.Value<global::random.IRandomSource>(), new int[0])), "generator" },
            new object[] { (TestDelegate)(() => RandomCollections.Shuffle(generator, (int[])NullReference.Value<int[]>())), "values" },
            new object[] { (TestDelegate)(() => RandomCollections.Shuffle(generator, (Array)NullReference.Value<Array>())), "values" },
            new object[] { (TestDelegate)(() => RandomCollections.Shuffle(generator, (IList)NullReference.Value<global::System.Collections.IList>())), "values" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }

        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void ObjectArrayShufflePreservesReferencesAndNullEntries()
    {
        object first = new object();
        object second = new object();
        object third = new object();
        object[] values = [first, NullReference.Value<object>(), second, third, NullReference.Value<object>()];
        object[] original = (object[])values.Clone();

        RandomCollections.Shuffle(Create(890UL), values);

        Assert.That(values, Is.EquivalentTo(original));
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

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
